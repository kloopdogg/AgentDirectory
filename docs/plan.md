# Agent Directory App — Implementation Plan

## Context

An internal company web app that serves as a directory of AI agents built by the team. Users can browse agents via a card-based UI, then enter a Microsoft Foundry-style playground to interact with any agent, view its full details, and get integration instructions. The app must support multiple agent protocols (OpenAI-compatible, A2A, MCP, custom REST), real-time streaming responses, multimodal input (file drop + screenshot paste), and Azure AD SSO authentication.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Frontend | Angular 19+, Angular Material, MSAL Angular (Azure AD) |
| Backend | ASP.NET Core 9, C# |
| Database | Azure SQL + EF Core |
| File storage | Azure Blob Storage (temp files for multimodal input) |
| Auth | Azure Entra ID (app registration, Bearer JWT) |
| Hosting | Azure App Service (one service hosts both API + static Angular build) |
| Streaming | Server-Sent Events (SSE) via `IAsyncEnumerable` in .NET 9 |

---

## Architecture

```
┌─────────────────────────────────────────────────────┐
│               Angular SPA (hosted via API)          │
│                                                     │
│  /directory  → Agent card grid                      │
│  /agent/:id  → Playground (chat + details)          │
│  /admin      → Admin panel (role-gated)             │
└──────────────────┬──────────────────────────────────┘
                   │ HTTP + SSE (Bearer JWT)
                   ▼
┌─────────────────────────────────────────────────────┐
│            ASP.NET Core 9 Web API                   │
│                                                     │
│  AgentsController   → registry CRUD (admin)         │
│  PlaygroundController → session + SSE stream        │
│                                                     │
│  AgentGateway (dispatcher)                          │
│    ├── OpenAIAdapter (chat completions / Responses) │
│    ├── A2AAdapter (v0.3 REST/JSON-RPC)              │
│    ├── MCPAdapter (HTTP+SSE transport)              │
│    └── CustomRestAdapter                            │
└──────┬────────────────────────────────────────────┬─┘
       │                                            │
  Azure SQL                              Agent endpoints
  (agent registry)                    (OpenAI / A2A / MCP
                                        / custom REST)
```

### Key Design Decisions

1. **Adapter pattern for multi-protocol support**: Each agent in the registry has a `ProtocolType` enum field. The `AgentGateway` dispatches to the correct adapter, which normalizes all output into a shared `AgentEvent` stream.

2. **SSE for streaming**: Backend exposes `GET /api/playground/{sessionId}/stream` returning `IAsyncEnumerable<AgentEvent>`. Angular consumes via `EventSource` wrapped in an RxJS Observable.

3. **Session management in-memory**: Sessions are stored in a `ConcurrentDictionary<string, AgentSession>` keyed by GUID. Cleared when user clicks "New Conversation" (DELETE endpoint) or on app restart. No DB persistence needed.

4. **File uploads**: Multipart POST to `/api/playground/{sessionId}/upload` → stored temporarily in Azure Blob Storage → included in message payload if agent supports multimodal.

5. **Azure AD auth**: MSAL Angular handles frontend token acquisition. Backend validates Bearer JWTs with Microsoft.Identity.Web. Admin role gated by Azure AD group membership claim.

---

## Project Structure

```
/AgentDirectory
  /AgentDirectory.Web         ← Angular app (builds to wwwroot of API)
    /src/app
      /core
        /auth                 ← MSAL config, auth guard
        /services             ← agent.service.ts, stream.service.ts, upload.service.ts
        /interceptors         ← auth token interceptor
      /features
        /directory            ← agent-list page + agent-card component
        /playground           ← playground page (chat + details + instructions)
        /admin                ← admin panel (CRUD agent registry)
      /shared
        /components           ← file-drop zone, code-snippet viewer, agent-badge
        /models               ← Agent, AgentEvent, Message interfaces

  /AgentDirectory.Api         ← ASP.NET Core 9 project
    /Controllers
      AgentsController.cs     ← GET (public), POST/PUT/DELETE (admin)
      PlaygroundController.cs ← session create, message send, SSE stream, file upload
    /Services
      AgentRegistryService.cs
      /Gateway
        AgentGateway.cs       ← protocol dispatcher
        IAgentAdapter.cs      ← common adapter interface
        OpenAIAdapter.cs
        A2AAdapter.cs
        MCPAdapter.cs
        CustomRestAdapter.cs
    /Models
      Agent.cs, AgentSession.cs, AgentEvent.cs, Message.cs

  /AgentDirectory.Data        ← EF Core project
    /Entities
      AgentEntity.cs
    AgentDbContext.cs
    AgentRepository.cs
```

---

## Database Schema (Azure SQL)

```sql
CREATE TABLE Agents (
    Id                 UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name               NVARCHAR(100) NOT NULL,
    ShortDescription   NVARCHAR(300) NOT NULL,
    LongDescription    NVARCHAR(MAX),
    EndpointUrl        NVARCHAR(500) NOT NULL,
    ProtocolType       NVARCHAR(50) NOT NULL,  -- OpenAI_Chat, OpenAI_Responses, A2A, MCP, CustomREST
    AuthType           NVARCHAR(50),           -- None, ApiKey, BearerToken, ManagedIdentity
    AuthSecretRef      NVARCHAR(200),          -- Key Vault reference for secrets
    SupportsMultimodal BIT DEFAULT 0,
    SupportsStreaming   BIT DEFAULT 1,
    Tags               NVARCHAR(500),
    Category           NVARCHAR(100),
    IconUrl            NVARCHAR(500),
    IsPublished        BIT DEFAULT 0,
    CreatedAt          DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt          DATETIME2 DEFAULT GETUTCDATE(),
    CreatedBy          NVARCHAR(200)
);
```

Secrets (API keys for agents) are stored in **Azure Key Vault**, referenced by `AuthSecretRef`. Never stored in the DB plain text.

---

## Feature Implementation Details

### 1. Directory Page
- Angular Material grid list of agent cards
- Each card: icon, name, category badge, short description, tags chips, "Try it" button
- Search bar + filter by category/tag
- Clicking card routes to `/agent/:id`

### 2. Playground Page

**Layout** (split panel):
- **Left panel**: Agent details (name, full description, capabilities, "How to integrate" accordion)
- **Right panel**: Chat interface

**Chat features:**
- Message bubbles (user right, agent left) with markdown rendering
- Token-by-token streaming with blinking cursor `▌`
- "New Conversation" button (clears session, starts fresh)
- File drop zone over the chat area (drag files in)
- Screenshot paste (clipboard paste in textarea captures images)
- Files/images show preview thumbnails before sending
- Tool call display (shows when agent is using a tool mid-response)
- Copy button on agent messages

**"How to Integrate" section** (collapsible accordion in left panel):
Shows dynamic code snippets based on `ProtocolType`:
- **OpenAI-compatible**: `curl`, Python, and C# examples
- **A2A**: Agent card URL + sample session/message JSON
- **MCP**: How to register as an MCP server in Claude Desktop or another MCP host

### 3. Admin Panel (`/admin`, role-gated)
- Agent list with published/draft status
- Create/edit form: all agent fields
- Toggle published/draft
- Preview card before publishing
- Azure Key Vault integration for secrets

### 4. Multi-Protocol Adapter Interface

```csharp
public interface IAgentAdapter
{
    AgentProtocol Protocol { get; }

    Task<string> StartSessionAsync(AgentEntity agent, CancellationToken ct);

    IAsyncEnumerable<AgentEvent> StreamMessageAsync(
        string sessionId,
        AgentEntity agent,
        string userMessage,
        IEnumerable<UploadedFile> files,
        CancellationToken ct);
}

public record AgentEvent(
    string Type,       // "token" | "tool_call" | "tool_result" | "done" | "error"
    string? Content,
    string? ToolName,
    string? Error
);
```

### 5. SSE Stream Endpoint (ASP.NET Core 9)

```csharp
[HttpGet("{sessionId}/stream")]
public async IAsyncEnumerable<AgentEvent> StreamAsync(
    string sessionId,
    [EnumeratorCancellation] CancellationToken ct)
{
    var session = _sessionManager.Get(sessionId);
    await foreach (var evt in _gateway.StreamAsync(session, ct))
        yield return evt;
}
```

### 6. Angular SSE Consumer

```typescript
streamSession(sessionId: string): Observable<AgentEvent> {
  return new Observable(sub => {
    const es = new EventSource(`/api/playground/${sessionId}/stream`);
    es.onmessage = e => sub.next(JSON.parse(e.data));
    es.onerror = () => { sub.error(); es.close(); };
    return () => es.close();
  });
}
```

---

## Azure Hosting

- **Azure App Service (Linux, .NET 9)**: Hosts the API. Angular build output copied to `wwwroot` and served via `UseStaticFiles()` + SPA fallback.
- **Azure SQL Database**: Agent registry
- **Azure Key Vault**: Agent secrets / API keys
- **Azure Blob Storage**: Temp file uploads (short-lived SAS URLs, auto-expired)
- **App Registrations**: One for the API (exposes scopes), one for the Angular SPA client (public client, PKCE)
- **Managed Identity**: App Service uses managed identity to access Key Vault and Blob Storage (no secrets in config)

---

## Implementation Phases

### Phase 1 — Core Foundation
- [ ] Set up Angular 19 project + ASP.NET Core 9 solution (mono-repo)
- [ ] Configure Azure AD auth (MSAL Angular + Microsoft.Identity.Web)
- [ ] EF Core + Azure SQL with `Agents` table + migrations
- [ ] AgentsController (GET list, GET by ID)
- [ ] Directory page with agent cards

### Phase 2 — Playground (OpenAI first)
- [ ] OpenAIAdapter (chat completions with streaming)
- [ ] In-memory session management
- [ ] SSE streaming endpoint + Angular EventSource consumer
- [ ] Playground page: chat UI with streaming, markdown rendering
- [ ] "New Conversation" flow

### Phase 3 — Multimodal + File Handling
- [ ] File drop zone + clipboard paste (Angular)
- [ ] File upload endpoint → Azure Blob Storage
- [ ] Pass files to OpenAI adapter as multimodal message content

### Phase 4 — Additional Protocols
- [ ] A2AAdapter (agent card discovery + session/message endpoints)
- [ ] MCPAdapter (HTTP+SSE transport)
- [ ] CustomRestAdapter (configurable request/response schema)
- [ ] "How to integrate" code snippet section

### Phase 5 — Admin + Polish
- [ ] Admin panel (CRUD agents, Key Vault secret registration)
- [ ] Category/tag filtering on directory
- [ ] Azure DevOps / GitHub Actions CI/CD pipeline

---

## Verification

1. **Auth**: Navigate to app → confirm Azure AD redirect → token in network requests
2. **Directory**: Agent cards render from database; search/filter works
3. **Playground streaming**: Send message to OpenAI-backed agent → token-by-token streaming appears
4. **Multimodal**: Drop image into chat → uploads → appears in payload sent to agent
5. **New Conversation**: Button clears session, fresh chat starts
6. **Admin**: Admin role user can create/publish an agent; it appears in directory
7. **A2A / MCP**: Test agent registered with A2A or MCP protocol routes to correct adapter
8. **Azure deploy**: App loads over HTTPS on App Service with correct auth redirect URIs
