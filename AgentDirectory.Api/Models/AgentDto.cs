namespace AgentDirectory.Api.Models;

public record AgentSummaryDto(
    Guid Id,
    string Name,
    string ShortDescription,
    string ProtocolType,
    bool SupportsMultimodal,
    bool SupportsStreaming,
    string? Tags,
    string? Category,
    string? IconUrl,
    bool IsPublished
);

public record AgentDetailDto(
    Guid Id,
    string Name,
    string ShortDescription,
    string? LongDescription,
    string EndpointUrl,
    string ProtocolType,
    string? AuthType,
    bool SupportsMultimodal,
    bool SupportsStreaming,
    string? Tags,
    string? Category,
    string? IconUrl,
    string? UsageInstructions,
    bool IsPublished,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string? CreatedBy
);

public record CreateAgentRequest(
    string Name,
    string ShortDescription,
    string? LongDescription,
    string EndpointUrl,
    string ProtocolType,
    string? AuthType,
    string? AuthSecretRef,
    bool SupportsMultimodal,
    bool SupportsStreaming,
    string? Tags,
    string? Category,
    string? IconUrl,
    string? UsageInstructions,
    bool IsPublished
);

public record UpdateAgentRequest(
    string Name,
    string ShortDescription,
    string? LongDescription,
    string EndpointUrl,
    string ProtocolType,
    string? AuthType,
    string? AuthSecretRef,
    bool SupportsMultimodal,
    bool SupportsStreaming,
    string? Tags,
    string? Category,
    string? IconUrl,
    string? UsageInstructions,
    bool IsPublished
);
