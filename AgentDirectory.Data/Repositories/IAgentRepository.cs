using AgentDirectory.Data.Entities;

namespace AgentDirectory.Data.Repositories;

public interface IAgentRepository
{
    Task<List<AgentEntity>> GetPublishedAsync(CancellationToken ct = default);
    Task<List<AgentEntity>> GetAllAsync(CancellationToken ct = default);
    Task<AgentEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<AgentEntity> CreateAsync(AgentEntity agent, CancellationToken ct = default);
    Task<AgentEntity?> UpdateAsync(AgentEntity agent, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
