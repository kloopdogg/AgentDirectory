using AgentDirectory.Api.Models;
using AgentDirectory.Data.Entities;
using AgentDirectory.Data.Repositories;

namespace AgentDirectory.Api.Services;

public class AgentRegistryService(IAgentRepository repository)
{
    public async Task<List<AgentSummaryDto>> GetPublishedAgentsAsync(CancellationToken ct = default)
    {
        var agents = await repository.GetPublishedAsync(ct);
        return agents.Select(ToSummary).ToList();
    }

    public async Task<List<AgentSummaryDto>> GetAllAgentsAsync(CancellationToken ct = default)
    {
        var agents = await repository.GetAllAsync(ct);
        return agents.Select(ToSummary).ToList();
    }

    public async Task<AgentDetailDto?> GetAgentAsync(Guid id, CancellationToken ct = default)
    {
        var agent = await repository.GetByIdAsync(id, ct);
        return agent is null ? null : ToDetail(agent);
    }

    public async Task<AgentDetailDto> CreateAgentAsync(CreateAgentRequest request, string createdBy, CancellationToken ct = default)
    {
        var entity = new AgentEntity
        {
            Name = request.Name,
            ShortDescription = request.ShortDescription,
            LongDescription = request.LongDescription,
            EndpointUrl = request.EndpointUrl,
            ProtocolType = request.ProtocolType,
            AuthType = request.AuthType,
            AuthSecretRef = request.AuthSecretRef,
            SupportsMultimodal = request.SupportsMultimodal,
            SupportsStreaming = request.SupportsStreaming,
            Tags = request.Tags,
            Category = request.Category,
            IconUrl = request.IconUrl,
            UsageInstructions = request.UsageInstructions,
            IsPublished = request.IsPublished,
            CreatedBy = createdBy
        };

        var created = await repository.CreateAsync(entity, ct);
        return ToDetail(created);
    }

    public async Task<AgentDetailDto?> UpdateAgentAsync(Guid id, UpdateAgentRequest request, CancellationToken ct = default)
    {
        var entity = new AgentEntity
        {
            Id = id,
            Name = request.Name,
            ShortDescription = request.ShortDescription,
            LongDescription = request.LongDescription,
            EndpointUrl = request.EndpointUrl,
            ProtocolType = request.ProtocolType,
            AuthType = request.AuthType,
            AuthSecretRef = request.AuthSecretRef,
            SupportsMultimodal = request.SupportsMultimodal,
            SupportsStreaming = request.SupportsStreaming,
            Tags = request.Tags,
            Category = request.Category,
            IconUrl = request.IconUrl,
            UsageInstructions = request.UsageInstructions,
            IsPublished = request.IsPublished
        };

        var updated = await repository.UpdateAsync(entity, ct);
        return updated is null ? null : ToDetail(updated);
    }

    public async Task<bool> DeleteAgentAsync(Guid id, CancellationToken ct = default) =>
        await repository.DeleteAsync(id, ct);

    private static AgentSummaryDto ToSummary(AgentEntity e) => new(
        e.Id, e.Name, e.ShortDescription, e.ProtocolType,
        e.SupportsMultimodal, e.SupportsStreaming, e.Tags, e.Category, e.IconUrl, e.IsPublished);

    private static AgentDetailDto ToDetail(AgentEntity e) => new(
        e.Id, e.Name, e.ShortDescription, e.LongDescription,
        e.EndpointUrl, e.ProtocolType, e.AuthType,
        e.SupportsMultimodal, e.SupportsStreaming,
        e.Tags, e.Category, e.IconUrl, e.UsageInstructions,
        e.IsPublished, e.CreatedAt, e.UpdatedAt, e.CreatedBy);
}
