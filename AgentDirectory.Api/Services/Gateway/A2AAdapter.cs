using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using AgentDirectory.Api.Models;
using AgentDirectory.Data.Entities;

namespace AgentDirectory.Api.Services.Gateway;

/// <summary>
/// Adapter for agents that implement the Google A2A Protocol v0.3.
/// Uses JSON-RPC 2.0 over HTTP with SSE streaming.
/// Spec: https://a2a-protocol.org/latest/specification/
/// </summary>
public class A2AAdapter(IHttpClientFactory httpClientFactory) : IAgentAdapter
{
    public string Protocol => "A2A";

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
        var client = httpClientFactory.CreateClient();
        if (!string.IsNullOrEmpty(apiKey))
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        // Send message via A2A session endpoint
        var messageUrl = $"{agent.EndpointUrl.TrimEnd('/')}/sessions/{sessionId}/messages";
        var payload = new
        {
            jsonrpc = "2.0",
            id = Guid.NewGuid().ToString(),
            method = "message/send",
            @params = new
            {
                message = new
                {
                    role = "user",
                    parts = new[] { new { text = userMessage } }
                }
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await client.PostAsync(messageUrl, content, ct);
        response.EnsureSuccessStatusCode();

        // Stream SSE events from the A2A endpoint
        var streamUrl = $"{agent.EndpointUrl.TrimEnd('/')}/sessions/{sessionId}/stream";
        using var streamResponse = await client.GetAsync(streamUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        using var stream = await streamResponse.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrEmpty(line) || !line.StartsWith("data: ")) continue;

            var data = line["data: ".Length..];
            if (data == "[DONE]") break;

            AgentEvent? evt = null;
            try
            {
                using var doc = JsonDocument.Parse(data);
                var root = doc.RootElement;
                if (root.TryGetProperty("type", out var typeEl))
                {
                    var type = typeEl.GetString();
                    evt = type switch
                    {
                        "content" => new AgentEvent("token", root.GetProperty("content").GetString(), null, null),
                        "tool_call" => new AgentEvent("tool_call", null, root.TryGetProperty("name", out var n) ? n.GetString() : null, null),
                        "done" => new AgentEvent("done", null, null, null),
                        _ => null
                    };
                }
            }
            catch { /* skip malformed events */ }

            if (evt is not null)
                yield return evt;
        }

        yield return new AgentEvent("done", null, null, null);
    }
}
