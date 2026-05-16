using AgentDirectory.Api.Models;
using AgentDirectory.Api.Services;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgentDirectory.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AgentsController(AgentRegistryService registryService, BlobServiceClient? blobServiceClient, IWebHostEnvironment env) : ControllerBase
{
    /// <summary>Returns all published agents (available to all authenticated users).</summary>
    [HttpGet]
    public async Task<ActionResult<List<AgentSummaryDto>>> GetPublished(CancellationToken ct) =>
        await registryService.GetPublishedAgentsAsync(ct);

    /// <summary>Returns full detail for one agent (published or not — used by playground).</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AgentDetailDto>> GetById(Guid id, CancellationToken ct)
    {
        var agent = await registryService.GetAgentAsync(id, ct);
        return agent is null ? NotFound() : Ok(agent);
    }

    // ── Admin endpoints ───────────────────────────────────────────────────────

    /// <summary>Returns ALL agents including drafts. Admin role required.</summary>
    [HttpGet("admin/all")]
    [Authorize(Roles = "AgentDirectoryAdmin")]
    public async Task<ActionResult<List<AgentSummaryDto>>> GetAll(CancellationToken ct) =>
        await registryService.GetAllAgentsAsync(ct);

    [HttpPost]
    [Authorize(Roles = "AgentDirectoryAdmin")]
    public async Task<ActionResult<AgentDetailDto>> Create([FromBody] CreateAgentRequest request, CancellationToken ct)
    {
        var upn = User.FindFirst("preferred_username")?.Value
                  ?? User.FindFirst("upn")?.Value
                  ?? "unknown";

        var created = await registryService.CreateAgentAsync(request, upn, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "AgentDirectoryAdmin")]
    public async Task<ActionResult<AgentDetailDto>> Update(Guid id, [FromBody] UpdateAgentRequest request, CancellationToken ct)
    {
        var updated = await registryService.UpdateAgentAsync(id, request, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "AgentDirectoryAdmin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await registryService.DeleteAgentAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>Uploads a PNG icon and returns its URL. Admin role required.</summary>
    [HttpPost("icon")]
    [Authorize(Roles = "AgentDirectoryAdmin")]
    [RequestSizeLimit(2 * 1024 * 1024)] // 2 MB
    public async Task<ActionResult<IconUploadResult>> UploadIcon(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file provided.");

        if (!string.Equals(file.ContentType, "image/png", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only PNG files are supported.");

        var fileName = $"{Guid.NewGuid():N}.png";

        // ── Azure Blob Storage (production) ──────────────────────────────
        if (blobServiceClient is not null)
        {
            var container = blobServiceClient.GetBlobContainerClient("agent-icons");
            await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: ct);

            var blob = container.GetBlobClient(fileName);
            await blob.UploadAsync(file.OpenReadStream(), new BlobHttpHeaders { ContentType = "image/png" }, cancellationToken: ct);

            return Ok(new IconUploadResult(blob.Uri.ToString()));
        }

        // ── Local fallback (development, no blob storage configured) ─────
        var folder = Path.Combine(env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "agent-icons");
        Directory.CreateDirectory(folder);

        var localPath = Path.Combine(folder, fileName);
        await using var stream = System.IO.File.Create(localPath);
        await file.CopyToAsync(stream, ct);

        var relativeUrl = $"/agent-icons/{fileName}";
        return Ok(new IconUploadResult(relativeUrl));
    }
}

public record IconUploadResult(string Url);
