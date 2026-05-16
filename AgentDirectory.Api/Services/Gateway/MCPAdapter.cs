using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using AgentDirectory.Api.Models;
using AgentDirectory.Data.Entities;

namespace AgentDirectory.Api.Services.Gateway;

/// <summary>
/// Adapter for agents exposed as MCP (Model Context Protocol) servers via HTTP+SSE transport.
/// Spec: https://modelcontextprotocol.io/specification
/// </summary>
public class MCPAdapter(IHttpClientFactory httpClientFactory) : IAgentAdapter
{
    public string Protocol => "MCP";

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

        // MCP tool call via JSON-RPC 2.0
        var mcpUrl = $"{agent.EndpointUrl.TrimEnd('/')}/messages";
        var request = new
        {
            jsonrpc = "2.0",
            id = Guid.NewGuid().ToString(),
            method = "tools/call",
            @params = new
            {
                name = "chat",
                arguments = new { message = userMessage, session_id = sessionId }
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        // MCP HTTP+SSE: POST triggers processing, SSE endpoint streams events
        await client.PostAsync(mcpUrl, content, ct);

        var streamUrl = $"{agent.EndpointUrl.TrimEnd('/')}/stream";
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
                if (root.TryGetProperty("result", out var result))
                {
                    if (result.TryGetProperty("content", out var contentEl))
                    {
                        foreach (var item in contentEl.EnumerateArray())
                        {
                            if (item.TryGetProperty("type", out var t) && t.GetString() == "text"
                                && item.TryGetProperty("text", out var text))
                            {
                                evt = new AgentEvent("token", text.GetString(), null, null);
                            }
                        }
                    }
                }
            }
            catch { /* skip malformed events */ }

            if (evt is not null)
                yield return evt;
        }

        yield return new AgentEvent("done", null, null, null);
    }
}
