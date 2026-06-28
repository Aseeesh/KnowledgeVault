using Microsoft.EntityFrameworkCore;
using KnowledgeVault.Core.Entities;
using KnowledgeVault.Core.Interfaces.Repositories;
using KnowledgeVault.Infrastructure.Data;

namespace KnowledgeVault.Infrastructure.Repositories;

public class TenantRepository(AppDbContext db) : ITenantRepository
{
    public async Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Tenants.FindAsync([id], ct);

    public async Task<Tenant?> GetBySlugAsync(string slug, CancellationToken ct = default) =>
        await db.Tenants.FirstOrDefaultAsync(t => t.Slug == slug, ct);

    public async Task<Tenant?> GetByApiKeyAsync(string apiKey, CancellationToken ct = default) =>
        await db.Tenants.FirstOrDefaultAsync(t => t.ApiKey == apiKey && t.IsActive, ct);

    public async Task<Tenant> CreateAsync(Tenant tenant, CancellationToken ct = default)
    {
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync(ct);
        return tenant;
    }
}
