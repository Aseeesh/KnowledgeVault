using KnowledgeVault.Core.Entities;

namespace KnowledgeVault.Core.Interfaces.Repositories;

public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Tenant?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<Tenant?> GetByApiKeyAsync(string apiKey, CancellationToken ct = default);
    Task<IReadOnlyList<Tenant>> GetAllAsync(CancellationToken ct = default);  // ✅ Add this
    Task<Tenant> CreateAsync(Tenant tenant, CancellationToken ct = default);
    Task<Tenant> UpdateAsync(Tenant tenant, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
}