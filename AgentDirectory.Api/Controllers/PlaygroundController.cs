using System.Text.Json;
using AgentDirectory.Api.Models;
using AgentDirectory.Api.Services.Gateway;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgentDirectory.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PlaygroundController(
    AgentGateway gateway,
    BlobServiceClient? blobServiceClient) : ControllerBase
{
    private const string BlobContainerName = "playground-uploads";

    /// <summary>Creates a new in-memory session for an agent.</summary>
    [HttpPost("sessions")]
    public ActionResult<CreateSessionResponse> CreateSession([FromBody] CreateSessionRequest request)
    {
        var session = gateway.CreateSession(request.AgentId);
        return Ok(new CreateSessionResponse(session.SessionId, session.AgentId));
    }

    /// <summary>Deletes a session (called when user clicks "New Conversation").</summary>
    [HttpDelete("sessions/{sessionId}")]
    public IActionResult DeleteSession(string sessionId)
    {
        gateway.DeleteSession(sessionId);
        return NoContent();
    }

    /// <summary>
    /// SSE stream endpoint. Writes text/event-stream so EventSource can consume it.
    /// </summary>
    [HttpGet("sessions/{sessionId}/stream")]
    public async Task Stream(
        string sessionId,
        [FromQuery] string message,
        CancellationToken ct)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await foreach (var evt in gateway.StreamAsync(sessionId, message, ct))
        {
            await Response.WriteAsync($"data: {JsonSerializer.Serialize(evt, options)}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }
    }

    /// <summary>
    /// Uploads a file (image or document) to Azure Blob Storage and associates
    /// it with the current session as a pending attachment.
    /// </summary>
    [HttpPost("sessions/{sessionId}/upload")]
    [RequestSizeLimit(20 * 1024 * 1024)] // 20 MB
    public async Task<ActionResult<UploadedFileInfo>> UploadFile(
        string sessionId,
        IFormFile file,
        CancellationToken ct)
    {
        var session = gateway.GetSession(sessionId);
        if (session is null) return NotFound("Session not found.");

        if (blobServiceClient is null)
            return StatusCode(503, "File upload is not configured (Azure Blob Storage not available).");

        var containerClient = blobServiceClient.GetBlobContainerClient(BlobContainerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: ct);

        var blobName = $"{sessionId}/{Guid.NewGuid()}/{file.FileName}";
        var blobClient = containerClient.GetBlobClient(blobName);

        await using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream, overwrite: true, cancellationToken: ct);

        // Generate a short-lived SAS URL (30 minutes — enough for the playground session)
        var sasUri = blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddMinutes(30));
        var fileInfo = new UploadedFileInfo(file.FileName, sasUri.ToString(), file.ContentType);

        gateway.AddPendingFile(sessionId, fileInfo);

        return Ok(fileInfo);
    }
}
