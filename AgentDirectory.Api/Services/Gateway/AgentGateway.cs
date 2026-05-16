using AgentDirectory.Api.Models;
using AgentDirectory.Data.Entities;
using AgentDirectory.Data.Repositories;
using Azure.Security.KeyVault.Secrets;

namespace AgentDirectory.Api.Services.Gateway;

public class AgentSession
{
    public string SessionId { get; init; } = Guid.NewGuid().ToString();
    public Guid AgentId { get; init; }
    public List<ChatMessage> History { get; } = [];
    public List<UploadedFileInfo> PendingFiles { get; } = [];
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public string? PreviousResponseId { get; set; }
    public string? ConversationId { get; set; }
}

/// <summary>
/// Singleton gateway that dispatches agent requests to the correct protocol adapter.
/// Uses IServiceScopeFactory to resolve scoped services (repository) per-request.
/// </summary>
public class AgentGateway(
    IEnumerable<IAgentAdapter> adapters,
    IServiceScopeFactory scopeFactory,
    SecretClient? secretClient,
    ILogger<AgentGateway> logger)
{
    private readonly Dictionary<string, IAgentAdapter> _adapters =
        adapters.ToDictionary(a => a.Protocol, StringComparer.OrdinalIgnoreCase);

    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, AgentSession> _sessions = new();

    public AgentSession CreateSession(Guid agentId)
    {
        var session = new AgentSession { AgentId = agentId };
        _sessions[session.SessionId] = session;
        return session;
    }

    public AgentSession? GetSession(string sessionId) =>
        _sessions.TryGetValue(sessionId, out var s) ? s : null;

    public bool DeleteSession(string sessionId) =>
        _sessions.TryRemove(sessionId, out _);

    public void AddPendingFile(string sessionId, UploadedFileInfo file)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
            session.PendingFiles.Add(file);
    }

    public async IAsyncEnumerable<AgentEvent> StreamAsync(
        string sessionId,
        string userMessage,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            yield return new AgentEvent("error", null, null, "Session not found.");
            yield break;
        }

        AgentEntity? agent;
        using (var scope = scopeFactory.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IAgentRepository>();
            agent = await repo.GetByIdAsync(session.AgentId, ct);
        }

        if (agent is null)
        {
            yield return new AgentEvent("error", null, null, "Agent not found.");
            yield break;
        }

        if (!_adapters.TryGetValue(agent.ProtocolType, out var adapter))
        {
            yield return new AgentEvent("error", null, null, $"No adapter for protocol '{agent.ProtocolType}'.");
            yield break;
        }

        var apiKey = await ResolveApiKeyAsync(agent, ct);
        var files = session.PendingFiles.ToList();
        session.PendingFiles.Clear();

        session.History.Add(new ChatMessage("user", userMessage));

        var responseBuilder = new System.Text.StringBuilder();

        await foreach (var evt in adapter.StreamMessageAsync(
            sessionId, agent, apiKey, session.History, userMessage, files, session.PreviousResponseId, session.ConversationId, ct))
        {
            if (evt.Type == "token" && evt.Content is not null)
                responseBuilder.Append(evt.Content);

            if (evt.Type == "response_id" && evt.Content is not null)
            {
                session.PreviousResponseId = evt.Content;
                continue;
            }

            if (evt.Type == "conversation_id" && evt.Content is not null)
            {
                session.ConversationId = evt.Content;
                continue;
            }

            yield return evt;
        }

        var responseText = responseBuilder.ToString();
        if (!string.IsNullOrEmpty(responseText))
            session.History.Add(new ChatMessage("assistant", responseText));
    }

    private async Task<string> ResolveApiKeyAsync(AgentEntity agent, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(agent.AuthSecretRef) || secretClient is null)
            return string.Empty;

        try
        {
            var secret = await secretClient.GetSecretAsync(agent.AuthSecretRef, cancellationToken: ct);
            return secret.Value.Value;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to resolve API key for agent {AgentId} from Key Vault.", agent.Id);
            return string.Empty;
        }
    }
}
