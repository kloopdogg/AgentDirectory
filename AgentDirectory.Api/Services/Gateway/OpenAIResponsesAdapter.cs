#pragma warning disable OPENAI001

using System.Runtime.CompilerServices;
using AgentDirectory.Api.Models;
using AgentDirectory.Data.Entities;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Core;
using OpenAI.Responses;

namespace AgentDirectory.Api.Services.Gateway;

/// <summary>
/// Adapter for agents published on Microsoft Foundry that expose the OpenAI Responses API.
///
/// Auth: TokenCredential injected from DI (ClientSecretCredential in appsettings,
///       DefaultAzureCredential via az login or managed identity).
///       API key auth is not supported by Foundry Agent Applications.
///
/// Conversation state: server-managed via ProjectConversation (see ADR-001).
///   On the first turn a ProjectConversation is created and its ID is emitted as a
///   "conversation_id" event so AgentGateway can persist it in AgentSession.
///   Subsequent turns pass the stored ID back in; only the new user message is sent —
///   the server owns the history.
///
/// EndpointUrl format (set in Admin panel) — either:
///   Query-param: {projectUrl}?agentName={name}&agentVersion={version}
///     e.g. https://Alamosa.services.ai.azure.com/api/projects/Shared?agentName=tool-user&agentVersion=2
///   Path-based (copied from Foundry portal):
///     e.g. https://Alamosa.services.ai.azure.com/api/projects/Shared/agents/tool-user/protocols/openai/v1/responses
///   agentVersion is optional in both formats (omit to use latest).
/// </summary>
public class OpenAIResponsesAdapter(TokenCredential credential, IHttpClientFactory httpClientFactory) : IAgentAdapter
{
    public string Protocol => "OpenAI_Responses";

    public async IAsyncEnumerable<AgentEvent> StreamMessageAsync(
        string sessionId,
        AgentEntity agent,
        string apiKey,
        IReadOnlyList<ChatMessage> history,
        string userMessage,
        IReadOnlyList<UploadedFileInfo> files,
        string? previousResponseId,
        string? conversationId,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var (projectUrl, agentRef) = ParseEndpointUrl(agent.EndpointUrl);
        var projectClient = new AIProjectClient(endpoint: projectUrl, tokenProvider: credential);
        var openAiClient = projectClient.GetProjectOpenAIClient();

        // First turn: create a server-managed conversation and emit its ID to the gateway.
        if (conversationId is null)
        {
            var conversation = await openAiClient.GetProjectConversationsClient()
                .CreateProjectConversationAsync(cancellationToken: ct);
            conversationId = conversation.Value.Id;
            yield return new AgentEvent("conversation_id", conversationId, null, null);
        }

        var client = openAiClient.GetProjectResponsesClientForAgent(agentRef, conversationId);

        // With server-managed history we only send the new user message.
        var options = await BuildUserMessageOptionsAsync(userMessage, files);

        string? newResponseId = null;

        await foreach (var update in client.CreateResponseStreamingAsync(options, ct))
        {
            switch (update)
            {
                case StreamingResponseOutputTextDeltaUpdate textDelta
                    when !string.IsNullOrEmpty(textDelta.Delta):
                    yield return new AgentEvent("token", textDelta.Delta, null, null);
                    break;

                case StreamingResponseFunctionCallArgumentsDeltaUpdate:
                    yield return new AgentEvent("tool_call", null, "function", null);
                    break;

                case StreamingResponseCompletedUpdate completed:
                    newResponseId = completed.Response?.Id;
                    break;
            }
        }

        if (newResponseId is not null)
            yield return new AgentEvent("response_id", newResponseId, null, null);

        yield return new AgentEvent("done", null, null, null);
    }

    /// <summary>
    /// Parses the admin-configured EndpointUrl into a project URI and AgentReference.
    ///
    /// Supported formats:
    ///   Query-param:  {projectUrl}?agentName={name}&amp;agentVersion={version}
    ///   Path-based:   {projectUrl}/agents/{name}/protocols/openai/v1/responses
    ///                 (the URL shown in the Foundry portal for a published agent)
    /// </summary>
    private static (Uri projectUrl, AgentReference agentRef) ParseEndpointUrl(string endpointUrl)
    {
        var uri = new Uri(endpointUrl);

        // Query-param format: ?agentName=foo[&agentVersion=n]
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        if (query["agentName"] is { } nameFromQuery)
        {
            var agentVersion = query["agentVersion"];
            var projectUrl = new Uri(uri.GetLeftPart(UriPartial.Path));
            var agentRef = agentVersion is not null
                ? new AgentReference(name: nameFromQuery, version: agentVersion)
                : new AgentReference(name: nameFromQuery);
            return (projectUrl, agentRef);
        }

        // Path format: .../agents/{name}/protocols/...
        const string marker = "/agents/";
        var agentsIdx = uri.AbsolutePath.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (agentsIdx >= 0)
        {
            var afterMarker = uri.AbsolutePath[(agentsIdx + marker.Length)..];
            var slash = afterMarker.IndexOf('/');
            var nameFromPath = slash >= 0 ? afterMarker[..slash] : afterMarker;
            var projectPath = uri.AbsolutePath[..agentsIdx];
            var projectUri = new Uri($"{uri.Scheme}://{uri.Authority}{projectPath}");
            return (projectUri, new AgentReference(name: nameFromPath));
        }

        throw new InvalidOperationException(
            $"EndpointUrl for OpenAI_Responses must include ?agentName=<name> or follow the " +
            $"path format .../agents/<name>/protocols/openai/v1/responses. Got: {endpointUrl}");
    }

    /// <summary>
    /// Builds options containing only the new user message.
    /// History is managed server-side by the ProjectConversation.
    /// </summary>
    private async Task<CreateResponseOptions> BuildUserMessageOptionsAsync(
        string userMessage,
        IReadOnlyList<UploadedFileInfo> files)
    {
        var options = new CreateResponseOptions();
        await AddUserMessageWithFilesAsync(options, userMessage, files);
        return options;
    }

    private async Task AddUserMessageWithFilesAsync(
        CreateResponseOptions options,
        string userMessage,
        IReadOnlyList<UploadedFileInfo> files)
    {
        var imageFiles = files.Where(f => f.ContentType.StartsWith("image/")).ToList();

        if (imageFiles.Count == 0)
        {
            options.InputItems.Add(ResponseItem.CreateUserMessageItem(userMessage));
            return;
        }

        var contentParts = new List<ResponseContentPart>
        {
            ResponseContentPart.CreateInputTextPart(userMessage)
        };

        // Download image bytes locally so the Foundry service receives inline data
        // rather than a URL it may not be able to reach (e.g. a local Azurite SAS URL).
        var httpClient = httpClientFactory.CreateClient();
        foreach (var file in imageFiles)
        {
            var bytes = await httpClient.GetByteArrayAsync(file.BlobUrl);
            var base64 = Convert.ToBase64String(bytes);
            var dataUri = new Uri($"data:{file.ContentType};base64,{base64}");
            contentParts.Add(ResponseContentPart.CreateInputImagePart(dataUri));
        }

        options.InputItems.Add(ResponseItem.CreateUserMessageItem(contentParts));
    }
}
