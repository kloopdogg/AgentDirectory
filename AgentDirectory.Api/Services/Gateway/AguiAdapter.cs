using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using AgentDirectory.Api.Models;
using AgentDirectory.Data.Entities;

namespace AgentDirectory.Api.Services.Gateway;

/// <summary>
/// Adapter for agents that implement the AG-UI (Agent-User Interaction) Protocol.
/// AG-UI is an event-driven protocol for real-time streaming between AI agents and frontends.
/// Spec: https://docs.ag-ui.com/
///
/// Event types handled:
///   TEXT_MESSAGE_START, TEXT_MESSAGE_CONTENT, TEXT_MESSAGE_END
///   TOOL_CALL_START, TOOL_CALL_END
///   STATE_SNAPSHOT, STATE_DELTA
///   RUN_STARTED, RUN_FINISHED, RUN_ERROR
/// </summary>
public class AguiAdapter(IHttpClientFactory httpClientFactory) : IAgentAdapter
{
    public string Protocol => "AG-UI";

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

        // AG-UI uses SSE — POST the run request, then consume the SSE stream
        var runUrl = $"{agent.EndpointUrl.TrimEnd('/')}/runs";
        var payload = new
        {
            thread_id = sessionId,
            messages = history.Select(h => new { role = h.Role, content = h.Content })
                              .Append(new { role = "user", content = userMessage })
                              .ToArray()
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Post, runUrl) { Content = content };
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
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

                if (!root.TryGetProperty("type", out var typeEl)) continue;
                var type = typeEl.GetString();

                evt = type switch
                {
                    "TEXT_MESSAGE_CONTENT" when root.TryGetProperty("delta", out var delta)
                        => new AgentEvent("token", delta.GetString(), null, null),

                    "TOOL_CALL_START" when root.TryGetProperty("tool_call_id", out _)
                        => new AgentEvent("tool_call", null,
                            root.TryGetProperty("tool_name", out var tn) ? tn.GetString() : "unknown", null),

                    "TOOL_CALL_END"
                        => new AgentEvent("tool_result", null, null, null),

                    "RUN_FINISHED"
                        => new AgentEvent("done", null, null, null),

                    "RUN_ERROR" when root.TryGetProperty("message", out var msg)
                        => new AgentEvent("error", null, null, msg.GetString()),

                    _ => null
                };
            }
            catch { /* skip malformed events */ }

            if (evt is not null)
                yield return evt;

            if (evt?.Type == "done" || evt?.Type == "error") yield break;
        }

        yield return new AgentEvent("done", null, null, null);
    }
}
