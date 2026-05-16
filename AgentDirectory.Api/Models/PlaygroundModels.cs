namespace AgentDirectory.Api.Models;

public record CreateSessionRequest(Guid AgentId);

public record CreateSessionResponse(string SessionId, Guid AgentId);

public record SendMessageRequest(string Message);

public record AgentEvent(
    string Type,       // token | tool_call | tool_result | done | error
    string? Content,
    string? ToolName,
    string? Error
);

public record UploadedFileInfo(string FileName, string BlobUrl, string ContentType);
