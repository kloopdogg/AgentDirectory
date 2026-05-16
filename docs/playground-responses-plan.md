# Playground → Foundry Agent via OpenAI Responses API

## Goal

Add a new `OpenAI_Responses` protocol adapter so the playground can communicate with a
Microsoft Foundry agent that exposes the OpenAI Responses API endpoint. The integration
uses `Azure.AI.Projects.OpenAI` (`ProjectResponsesClient`) to avoid raw `HttpClient`
plumbing — the SDK handles SSE parsing, typed event dispatch, and auth.

---

## How It Fits the Existing Architecture

No changes to the controller, gateway, or Angular frontend. The gateway already routes
by `agent.ProtocolType` string — a new adapter is all that's needed.

```
PlaygroundController (unchanged)
  └── AgentGateway (unchanged)
        └── [routes "OpenAI_Responses" to new adapter]
              └── OpenAIResponsesAdapter  ← new
                    └── ProjectResponsesClient (Azure.AI.Projects.OpenAI)
                          └── Foundry agent Responses endpoint
```

---

## New NuGet Packages

Add to `AgentDirectory.Api/AgentDirectory.Api.csproj`:

| Package | Version | Purpose |
|---|---|---|
| `Azure.AI.Projects` | `2.0.0` (stable) | `AIProjectClient` — Foundry project client |
| `Azure.AI.Projects.OpenAI` | `2.0.0-beta.*` (latest) | `ProjectResponsesClient` — typed Responses API wrapper |
| `Azure.Identity` | already present | `DefaultAzureCredential` for Entra ID auth |

---

## Files That Change

| File | Change |
|---|---|
| `AgentDirectory.Api/Services/Gateway/OpenAIResponsesAdapter.cs` | **New** — the adapter |
| `AgentDirectory.Api/Services/Gateway/AgentGateway.cs` | Add `string? PreviousResponseId` to `AgentSession` |
| `AgentDirectory.Api/Program.cs` | Register `OpenAIResponsesAdapter` as singleton |
| `AgentDirectory.Api/AgentDirectory.Api.csproj` | Add two NuGet package references |

No changes to: `IAgentAdapter`, `PlaygroundController`, `AgentEvent`, Angular code.

---

## Multi-Turn Conversation State

The Responses API supports two approaches:

**Option A — Stateless** (send full history each turn as `input` array)
Works today with no session changes. Less efficient as history grows. Simple to
implement first.

**Option B — Stateful via `previous_response_id`** (recommended, implement from the start)
Only send the new user message each turn. The Foundry endpoint holds full conversation
context. More efficient. Requires tracking the last `response_id` per session.

**Plan:** Implement stateful (Option B). Add `PreviousResponseId` to `AgentSession`:

```csharp
// AgentGateway.cs — AgentSession
public class AgentSession
{
    public string SessionId { get; init; } = Guid.NewGuid().ToString();
    public Guid AgentId { get; init; }
    public List<ChatMessage> History { get; } = [];
    public List<UploadedFileInfo> PendingFiles { get; } = [];
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public string? PreviousResponseId { get; set; }  // ← new
}
```

The adapter needs to read and write `PreviousResponseId`. Since `IAgentAdapter` only
receives `history` (not the session object), the cleanest non-breaking approach is:

- The adapter yields a `new AgentEvent("response_id", responseId, null, null)` as its
  final event before `"done"`.
- `AgentGateway.StreamAsync` intercepts this event, stores it in `session.PreviousResponseId`,
  and does **not** forward it to the client (already ignored by Angular for unknown types).

On each subsequent turn, the gateway passes `session.PreviousResponseId` into the adapter
via a new optional parameter, or (simpler) as an overloaded meaning of the existing
`sessionId` parameter by having the adapter look it up... 

Actually simpler: extend `IAgentAdapter.StreamMessageAsync` with a nullable
`previousResponseId` parameter (default `null`). Existing adapters add `string?
previousResponseId = null` to their signatures — no behavioral change. The gateway passes
`session.PreviousResponseId` on each call.

**Updated interface signature:**

```csharp
IAsyncEnumerable<AgentEvent> StreamMessageAsync(
    string sessionId,
    AgentEntity agent,
    string apiKey,
    IReadOnlyList<ChatMessage> history,
    string userMessage,
    IReadOnlyList<UploadedFileInfo> files,
    string? previousResponseId,   // ← new, default null
    CancellationToken ct);
```

All existing adapters add `string? previousResponseId = null` and ignore it.

---

## New Adapter: `OpenAIResponsesAdapter`

```csharp
public class OpenAIResponsesAdapter : IAgentAdapter
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
        [EnumeratorCancellation] CancellationToken ct)
    {
        var responsesClient = BuildClient(agent, apiKey);

        var options = new ResponseCreationOptions { Stream = true };

        if (previousResponseId is not null)
        {
            // Stateful: server holds context, only send new message
            options.PreviousResponseId = previousResponseId;
            options.Input.Add(ResponseItem.CreateUserMessageItem(userMessage));
        }
        else
        {
            // First turn: send full history as input array
            foreach (var msg in history)
                options.Input.Add(msg.Role == "user"
                    ? ResponseItem.CreateUserMessageItem(msg.Content)
                    : ResponseItem.CreateAssistantMessageItem(msg.Content));

            // Include any image attachments on the final user message
            if (files.Count > 0)
                // add image URL parts alongside the text (see implementation notes)
        }

        string? responseId = null;

        await foreach (var update in responsesClient.CreateResponseStreamingAsync(options, ct))
        {
            switch (update)
            {
                case StreamingResponseTextDeltaUpdate delta:
                    yield return new AgentEvent("token", delta.Delta, null, null);
                    break;

                case StreamingResponseFunctionCallArgumentsDeltaUpdate fnDelta:
                    yield return new AgentEvent("tool_call", null, fnDelta.FunctionName, null);
                    break;

                case StreamingResponseCompletedUpdate completed:
                    responseId = completed.Response.Id;
                    break;
            }
        }

        if (responseId is not null)
            yield return new AgentEvent("response_id", responseId, null, null);

        yield return new AgentEvent("done", null, null, null);
    }

    private static ProjectResponsesClient BuildClient(AgentEntity agent, string apiKey)
    {
        // agent.EndpointUrl = "https://{account}.services.ai.azure.com/api/projects/{project}"
        var projectClient = agent.AuthType switch
        {
            "ApiKey" => new AIProjectClient(new Uri(agent.EndpointUrl), new ApiKeyCredential(apiKey)),
            _        => new AIProjectClient(new Uri(agent.EndpointUrl), new DefaultAzureCredential())
        };
        return projectClient.GetOpenAIResponsesClient();
    }
}
```

*Note: Exact streaming update type names (`StreamingResponseTextDeltaUpdate`, etc.) to be
confirmed against the `Azure.AI.Projects.OpenAI` beta API surface when the package is
added. The Responses SSE event model maps directly to typed update objects — no manual
JSON parsing.*

---

## `AgentGateway.StreamAsync` Changes

Two small additions:

1. Pass `session.PreviousResponseId` to the adapter call.
2. Intercept `"response_id"` events — store in session, do not forward downstream.

```csharp
// Pass previousResponseId
await foreach (var evt in adapter.StreamMessageAsync(
    sessionId, agent, apiKey, session.History, userMessage, files,
    session.PreviousResponseId, ct))   // ← new arg
{
    if (evt.Type == "token" && evt.Content is not null)
        responseBuilder.Append(evt.Content);

    if (evt.Type == "response_id" && evt.Content is not null)
    {
        session.PreviousResponseId = evt.Content;
        continue;  // do not forward to client
    }

    yield return evt;
}
```

"New Conversation" already calls `DeleteSession` and creates a fresh `AgentSession`,
which resets `PreviousResponseId` to `null` automatically.

---

## Authentication

`agent.AuthType` drives how `ProjectResponsesClient` is constructed:

| `AuthType` | Credential used | When to use |
|---|---|---|
| `"BearerToken"` / `"ManagedIdentity"` | `DefaultAzureCredential` | Prod (managed identity, `az login` in dev) |
| `"ApiKey"` | `ApiKeyCredential(apiKey)` | Dev/testing with a Foundry API key |

For local dev: set `AuthType = "ApiKey"`, put the Foundry API key in `AuthSecretRef` as a
Key Vault reference, or hardcode temporarily in the mock agent record.

---

## Agent Record Configuration (via Admin Panel)

Create a new agent entry with:

| Field | Value |
|---|---|
| `ProtocolType` | `OpenAI_Responses` |
| `EndpointUrl` | `https://{account}.services.ai.azure.com/api/projects/{project}` |
| `AuthType` | `ApiKey` (dev) or `ManagedIdentity` (prod) |
| `AuthSecretRef` | Key Vault ref (e.g. `foundry-api-key`) or blank for `DefaultAzureCredential` |
| `SupportsStreaming` | `true` |
| `SupportsMultimodal` | `false` initially, `true` once image inputs are verified |

---

## Implementation Steps

1. **Add NuGet packages** to `AgentDirectory.Api.csproj`
2. **Extend `AgentSession`** — add `string? PreviousResponseId`
3. **Update `IAgentAdapter`** — add `string? previousResponseId` parameter
4. **Update all existing adapters** — add the parameter with `= null`, ignore it
   - `OpenAIChatAdapter`, `A2AAdapter`, `MCPAdapter`, `CustomRestAdapter`, `AguiAdapter`, `MockAgentAdapter`
5. **Update `AgentGateway.StreamAsync`** — pass and intercept `previousResponseId`
6. **Create `OpenAIResponsesAdapter.cs`** — new adapter (confirm type names from SDK)
7. **Register adapter** in `Program.cs`
8. **Create agent record** in Admin panel pointing at the Foundry endpoint
9. **Smoke test** — new conversation, multi-turn (verify `PreviousResponseId` chains correctly)

---

## What Is NOT Changing

- `PlaygroundController` — untouched
- Angular frontend — untouched (ignores unknown event types)
- `AgentEvent` record — untouched
- All other adapters — signature update only, zero behavioral change
- Database schema — `ProtocolType` is already a free-form string, `OpenAI_Responses` works as-is
