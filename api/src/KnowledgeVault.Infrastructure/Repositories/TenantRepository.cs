using Microsoft.EntityFrameworkCore;
using KnowledgeVault.Core.Entities;
using KnowledgeVault.Core.Interfaces.Repositories;
using KnowledgeVault.Infrastructure.Data;

namespace KnowledgeVault.Infrastructure.Repositories;

public class TenantRepository : ITenantRepository
{
    private readonly AppDbContext _context;

    public TenantRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task<Tenant?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        return await _context.Tenants
            .FirstOrDefaultAsync(t => t.Slug == slug, ct);
    }

    public async Task<Tenant?> GetByApiKeyAsync(string apiKey, CancellationToken ct = default)
    {
        return await _context.Tenants
            .FirstOrDefaultAsync(t => t.ApiKey == apiKey, ct);
    }

    public async Task<IReadOnlyList<Tenant>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Tenants
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);
    }

    public async Task<Tenant> CreateAsync(Tenant tenant, CancellationToken ct = default)
    {
        tenant.CreatedAt = DateTime.UtcNow;
        tenant.UpdatedAt = DateTime.UtcNow;
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync(ct);
        return tenant;
    }

    public async Task<Tenant> UpdateAsync(Tenant tenant, CancellationToken ct = default)
    {
        tenant.UpdatedAt = DateTime.UtcNow;
        _context.Tenants.Update(tenant);
        await _context.SaveChangesAsync(ct);
        return tenant;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var tenant = await GetByIdAsync(id, ct);
        if (tenant is null) return false;
        
        tenant.IsActive = false;
        tenant.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Tenants.AnyAsync(t => t.Id == id, ct);
    }
}