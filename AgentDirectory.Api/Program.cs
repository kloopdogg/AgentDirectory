using AgentDirectory.Api.Infrastructure;
using AgentDirectory.Api.Services;
using AgentDirectory.Api.Services.Gateway;
using AgentDirectory.Data;
using AgentDirectory.Data.Repositories;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

// ── Auth ───────────────────────────────────────────────────────────────────
// Set Auth:Disabled = true in appsettings.Development.json to bypass Azure AD
// during local development. All other auth code remains in place.
var authDisabled = builder.Configuration.GetValue<bool>("Auth:Disabled");

if (authDisabled)
{
    // No-op auth: every request is treated as authenticated with an "admin" role
    builder.Services.AddAuthentication("DevBypass")
        .AddScheme<AuthenticationSchemeOptions, DevBypassAuthHandler>("DevBypass", _ => { });
    builder.Services.AddAuthorization(opts =>
    {
        opts.AddPolicy("default", p => p.RequireAuthenticatedUser());
        opts.DefaultPolicy = opts.GetPolicy("default")!;
    });
}
else
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
}

// ── Mock mode (no SQL Server, no real agents required) ────────────────────
var useMockData = builder.Configuration.GetValue<bool>("Data:UseMockData");

if (useMockData)
{
    // In-memory repository — no DB connection needed
    builder.Services.AddSingleton<IAgentRepository, MockAgentRepository>();
}
else
{
    // ── Database (Azure SQL via EF Core) ──────────────────────────────────
    builder.Services.AddDbContext<AgentDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddScoped<AgentRepository>();
    builder.Services.AddScoped<IAgentRepository>(sp => sp.GetRequiredService<AgentRepository>());
}

// ── Data layer ────────────────────────────────────────────────────────────
builder.Services.AddScoped<AgentRegistryService>();

// ── Agent Gateway (singleton — holds in-memory sessions) ─────────────────
builder.Services.AddSingleton<AgentGateway>();
builder.Services.AddHttpClient();

// ── Foundry credential (used by OpenAIResponsesAdapter) ──────────────────
// ClientSecretCredential reads TenantId/ClientId/ClientSecret from AzureAd config.
// In production replace the ClientSecret with a managed identity
// (remove ClientSecret from config and switch to new DefaultAzureCredential()).
var tenantId     = builder.Configuration["AzureAd:TenantId"];
var clientId     = builder.Configuration["AzureAd:ClientId"];
var clientSecret = builder.Configuration["AzureAd:ClientSecret"];

Azure.Core.TokenCredential foundryCredential = !string.IsNullOrEmpty(clientSecret)
    ? new Azure.Identity.ClientSecretCredential(tenantId, clientId, clientSecret)
    : new Azure.Identity.DefaultAzureCredential();

builder.Services.AddSingleton(foundryCredential);

// Register protocol adapters
builder.Services.AddSingleton<IAgentAdapter, OpenAIResponsesAdapter>();
// builder.Services.AddSingleton<IAgentAdapter, OpenAIChatAdapter>();
// builder.Services.AddSingleton<IAgentAdapter, A2AAdapter>();
// builder.Services.AddSingleton<IAgentAdapter, MCPAdapter>();
// builder.Services.AddSingleton<IAgentAdapter, CustomRestAdapter>();
// builder.Services.AddSingleton<IAgentAdapter, AguiAdapter>();
// builder.Services.AddSingleton<IAgentAdapter, MockAgentAdapter>();

// ── Azure services (optional — gracefully degrade when not configured) ────
var keyVaultUri = builder.Configuration["KeyVault:Uri"];
if (!string.IsNullOrEmpty(keyVaultUri))
    builder.Services.AddSingleton(new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential()));
else
    builder.Services.AddSingleton<SecretClient>(_ => null!);

var blobConnectionString = builder.Configuration.GetConnectionString("BlobStorage");
if (!string.IsNullOrEmpty(blobConnectionString))
    builder.Services.AddSingleton(new BlobServiceClient(blobConnectionString));
else
    builder.Services.AddSingleton<BlobServiceClient>(_ => null!);

// ── API + OpenAPI ─────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// ── CORS (for local dev when Angular runs on a different port) ────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalDev", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors("LocalDev");
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ── Serve Angular SPA (production) ───────────────────────────────────────
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

// ── Apply EF Core migrations on startup (skipped in mock mode) ───────────
if (!useMockData)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AgentDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();
