using AgentDirectory.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgentDirectory.Data.Repositories;

public class AgentRepository(AgentDbContext db) : IAgentRepository
{
    public async Task<List<AgentEntity>> GetPublishedAsync(CancellationToken ct = default) =>
        await db.Agents
            .Where(a => a.IsPublished)
            .OrderBy(a => a.Name)
            .ToListAsync(ct);

    public async Task<List<AgentEntity>> GetAllAsync(CancellationToken ct = default) =>
        await db.Agents
            .OrderBy(a => a.Name)
            .ToListAsync(ct);

    public async Task<AgentEntity?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Agents.FindAsync([id], ct);

    public async Task<AgentEntity> CreateAsync(AgentEntity agent, CancellationToken ct = default)
    {
        db.Agents.Add(agent);
        await db.SaveChangesAsync(ct);
        return agent;
    }

    public async Task<AgentEntity?> UpdateAsync(AgentEntity agent, CancellationToken ct = default)
    {
        var existing = await db.Agents.FindAsync([agent.Id], ct);
        if (existing is null) return null;

        existing.Name = agent.Name;
        existing.ShortDescription = agent.ShortDescription;
        existing.LongDescription = agent.LongDescription;
        existing.EndpointUrl = agent.EndpointUrl;
        existing.ProtocolType = agent.ProtocolType;
        existing.AuthType = agent.AuthType;
        existing.AuthSecretRef = agent.AuthSecretRef;
        existing.SupportsMultimodal = agent.SupportsMultimodal;
        existing.SupportsStreaming = agent.SupportsStreaming;
        existing.Tags = agent.Tags;
        existing.Category = agent.Category;
        existing.IconUrl = agent.IconUrl;
        existing.UsageInstructions = agent.UsageInstructions;
        existing.IsPublished = agent.IsPublished;
        existing.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var existing = await db.Agents.FindAsync([id], ct);
        if (existing is null) return false;

        db.Agents.Remove(existing);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
