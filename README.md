# Agent Directory

An internal company web app for browsing, discovering, and interacting with AI agents. Also facilitates inventory, governance, and observability of the agents.

## What it does

- **Directory** — Card-based grid of published agents with search and category filtering
- **Playground** — Microsoft Foundry-style chat interface per agent with real-time streaming, file drop, screenshot paste, tool call display, and a "How to integrate" panel with code snippets
- **Admin panel** — Full CRUD for the agent registry (admin role required)

## Tech stack

| Layer | Technology |
|---|---|
| Frontend | Angular 19, Angular Material, MSAL Angular |
| Backend | ASP.NET Core 9, C# |
| Database | Azure SQL + EF Core |
| File storage | Azure Blob Storage |
| Auth | Azure Entra ID (MSAL / Bearer JWT) |
| Hosting | Azure App Service |
| Streaming | Server-Sent Events (SSE) |

## Supported agent protocols

| Protocol | Description |
|---|---|
| `OpenAI_Chat` | OpenAI-compatible chat completions endpoint |
| `OpenAI_Responses` | OpenAI Responses API (stateful, tool-native) |
| `A2A` | Google Agent-to-Agent Protocol v0.3 |
| `MCP` | Model Context Protocol (HTTP+SSE transport) |
| `AG-UI` | Agent-User Interaction Protocol |
| `CustomREST` | Any custom REST endpoint |

## Project structure

```
/AgentDirectory
  /AgentDirectory.Api         ← ASP.NET Core 9 Web API
    /Controllers              ← AgentsController, PlaygroundController
    /Infrastructure           ← DevBypassAuthHandler (dev-only auth skip)
    /Services
      /Gateway                ← AgentGateway + 5 protocol adapters
  /AgentDirectory.Data        ← EF Core data layer
    /Entities                 ← AgentEntity
    /Migrations               ← EF Core migrations
    /Repositories             ← AgentRepository
  /AgentDirectory.Web         ← Angular 19 SPA
    /src/app
      /core
        /auth                 ← MSAL config
        /models               ← TypeScript interfaces
        /services             ← AgentService, PlaygroundService
      /features
        /directory            ← Agent card grid
        /playground           ← Chat interface
        /admin                ← Admin panel + editor dialog
      /shared/components      ← FileDropComponent
  /docs/plan.md               ← Full implementation plan
```

## Running locally

### Prerequisites
- .NET 9 SDK
- Node.js 20+ / npm
- SQL Server LocalDB (or update the connection string)
- Angular CLI (`npm install -g @angular/cli`)

### Backend

```bash
cd AgentDirectory.Api
dotnet run
```

The API starts on `https://localhost:7xxx`. Auth is bypassed in development — every request is treated as an authenticated admin (`Auth:Disabled: true` in `appsettings.Development.json`).

To apply database migrations on first run, the app calls `MigrateAsync()` on startup automatically. Alternatively:

```bash
dotnet ef database update --project AgentDirectory.Data --startup-project AgentDirectory.Api
```

### Frontend

```bash
cd AgentDirectory.Web
npm install
ng serve
```

The Angular dev server starts on `http://localhost:4200` with a proxy to the API.

## Enabling Azure AD authentication

1. Create two Azure App Registrations in Entra ID:
   - **API app** — exposes a scope (e.g. `access_as_user`)
   - **SPA app** — public client (PKCE), redirect URI = your app URL

2. Assign the `AgentDirectoryAdmin` app role in the API app registration

3. Fill in the placeholder values in:
   - `AgentDirectory.Api/appsettings.json` — `TenantId`, `ClientId`, `ClientSecret`, `Audience`
   - `AgentDirectory.Web/src/environments/environment.ts` — `clientId`, `tenantId`, `apiScopes`

4. Disable the dev auth bypass:
   ```json
   // appsettings.Development.json
   "Auth": { "Disabled": false }
   ```

5. Re-enable `MsalGuard` on routes in `AgentDirectory.Web/src/app/app.routes.ts`

## Adding an agent

1. Navigate to `/admin`
2. Click **New agent** and fill in:
   - Name, description, category, tags
   - Endpoint URL and protocol type
   - Auth type and Azure Key Vault secret name (if the agent requires an API key)
   - Capabilities (streaming, multimodal)
3. Toggle **Published** to make it appear in the directory

Agent API keys are never stored in the database — they're referenced by secret name and resolved from **Azure Key Vault** at runtime using the App Service's managed identity.

## Azure deployment

The Angular build output is served as static files from the ASP.NET Core app:

```bash
# Build Angular
cd AgentDirectory.Web
ng build --configuration=production

# Copy dist to API wwwroot
cp -r dist/AgentDirectory.Web/browser/* ../AgentDirectory.Api/wwwroot/

# Publish and deploy
cd ../AgentDirectory.Api
dotnet publish -c Release -o publish
# Deploy /publish to Azure App Service
```

Required Azure resources:
- **Azure App Service** (Linux, .NET 9)
- **Azure SQL Database**
- **Azure Key Vault** (for agent API keys)
- **Azure Blob Storage** (for playground file uploads)
- **Managed Identity** on the App Service (for Key Vault + Blob access — no secrets in config)
