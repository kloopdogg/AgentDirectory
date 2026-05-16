using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgentDirectory.Data.Entities;

[Table("Agents")]
public class AgentEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string ShortDescription { get; set; } = string.Empty;

    public string? LongDescription { get; set; }

    [Required, MaxLength(500)]
    public string EndpointUrl { get; set; } = string.Empty;

    /// <summary>OpenAI_Chat | OpenAI_Responses | A2A | MCP | CustomREST</summary>
    [Required, MaxLength(50)]
    public string ProtocolType { get; set; } = "OpenAI_Chat";

    /// <summary>None | ApiKey | BearerToken | ManagedIdentity</summary>
    [MaxLength(50)]
    public string? AuthType { get; set; }

    /// <summary>Azure Key Vault secret reference (e.g. keyvault://my-vault/secret-name)</summary>
    [MaxLength(200)]
    public string? AuthSecretRef { get; set; }

    public bool SupportsMultimodal { get; set; } = false;

    public bool SupportsStreaming { get; set; } = true;

    /// <summary>JSON array of tag strings</summary>
    public string? Tags { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    [MaxLength(500)]
    public string? IconUrl { get; set; }

    /// <summary>Usage instructions shown in the playground's "How to integrate" panel</summary>
    public string? UsageInstructions { get; set; }

    public bool IsPublished { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Azure AD UPN of the user who created this agent entry</summary>
    [MaxLength(200)]
    public string? CreatedBy { get; set; }
}
