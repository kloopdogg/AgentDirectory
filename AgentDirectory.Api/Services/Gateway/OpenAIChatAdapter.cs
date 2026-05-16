using System.ClientModel;
using System.Runtime.CompilerServices;
using AgentDirectory.Api.Models;
using AgentDirectory.Data.Entities;
using OpenAI;
using OpenAI.Chat;

namespace AgentDirectory.Api.Services.Gateway;

/// <summary>
/// Adapter for agents that expose an OpenAI-compatible Chat Completions endpoint.
/// Supports streaming via SSE (IAsyncEnumerable).
/// </summary>
public class OpenAIChatAdapter : IAgentAdapter
{
    public string Protocol => "OpenAI_Chat";

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
        var clientOptions = new OpenAIClientOptions { Endpoint = new Uri(agent.EndpointUrl) };
        var credential = new ApiKeyCredential(string.IsNullOrEmpty(apiKey) ? "placeholder" : apiKey);
        var client = new ChatClient(model: "gpt-4o", credential: credential, options: clientOptions);

        var messages = BuildMessages(history, userMessage, files);

        await foreach (var update in client.CompleteChatStreamingAsync(messages, cancellationToken: ct))
        {
            foreach (var part in update.ContentUpdate)
            {
                if (!string.IsNullOrEmpty(part.Text))
                    yield return new AgentEvent("token", part.Text, null, null);
            }

            if (update.FinishReason == ChatFinishReason.ToolCalls)
            {
                foreach (var toolCall in update.ToolCallUpdates)
                {
                    yield return new AgentEvent("tool_call", null, toolCall.FunctionName, null);
                }
            }
        }

        yield return new AgentEvent("done", null, null, null);
    }

    private static List<OpenAI.Chat.ChatMessage> BuildMessages(
        IReadOnlyList<ChatMessage> history,
        string userMessage,
        IReadOnlyList<UploadedFileInfo> files)
    {
        var messages = new List<OpenAI.Chat.ChatMessage>();

        foreach (var h in history)
        {
            if (h.Role == "user")
                messages.Add(new UserChatMessage(h.Content));
            else if (h.Role == "assistant")
                messages.Add(new AssistantChatMessage(h.Content));
        }

        if (files.Count > 0)
        {
            var parts = new List<ChatMessageContentPart> { ChatMessageContentPart.CreateTextPart(userMessage) };
            foreach (var file in files)
            {
                if (file.ContentType.StartsWith("image/"))
                    parts.Add(ChatMessageContentPart.CreateImagePart(new Uri(file.BlobUrl), ChatImageDetailLevel.Auto));
            }
            messages.Add(new UserChatMessage(parts));
        }
        else
        {
            messages.Add(new UserChatMessage(userMessage));
        }

        return messages;
    }
}
