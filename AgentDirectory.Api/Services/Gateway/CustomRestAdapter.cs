using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using AgentDirectory.Api.Models;
using AgentDirectory.Data.Entities;

namespace AgentDirectory.Api.Services.Gateway;

/// <summary>
/// Adapter for agents that expose a custom REST endpoint.
/// Expects a simple { "message": "...", "session_id": "..." } POST body
/// and either a JSON response { "response": "..." } or an SSE stream.
/// </summary>
public class CustomRestAdapter(IHttpClientFactory httpClientFactory) : IAgentAdapter
{
    public string Protocol => "CustomREST";

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

        var payload = new { message = userMessage, session_id = sessionId };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await client.PostAsync(agent.EndpointUrl, content, ct);
        response.EnsureSuccessStatusCode();

        var contentType = response.Content.Headers.ContentType?.MediaType ?? "";

        if (contentType.Contains("text/event-stream"))
        {
            // Handle SSE streaming response
            using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream && !ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(ct);
                if (string.IsNullOrEmpty(line) || !line.StartsWith("data: ")) continue;
                var data = line["data: ".Length..];
                if (data == "[DONE]") break;
                yield return new AgentEvent("token", data, null, null);
            }
        }
        else
        {
            // Handle standard JSON response
            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var text = doc.RootElement.TryGetProperty("response", out var r) ? r.GetString() : json;
            if (!string.IsNullOrEmpty(text))
                yield return new AgentEvent("token", text, null, null);
        }

        yield return new AgentEvent("done", null, null, null);
    }
}
