using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace AgentDirectory.Api.Infrastructure;

/// <summary>
/// Development-only authentication handler that treats every request as authenticated
/// with the AgentDirectoryAdmin role. Enabled via Auth:Disabled = true in appsettings.
///
/// NEVER use this in production — it bypasses all security checks.
/// </summary>
public class DevBypassAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "dev-user@localhost"),
            new Claim(ClaimTypes.Email, "dev-user@localhost"),
            new Claim("preferred_username", "dev-user@localhost"),
            new Claim(ClaimTypes.Role, "AgentDirectoryAdmin"),
        };

        var identity = new ClaimsIdentity(claims, "DevBypass");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "DevBypass");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
