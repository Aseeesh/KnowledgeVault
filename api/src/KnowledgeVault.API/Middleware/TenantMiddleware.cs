using KnowledgeVault.Application.Common;
using KnowledgeVault.Core.Interfaces.Repositories;

namespace KnowledgeVault.API.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITenantRepository tenantRepo, TenantContext tenantContext)
    {
        // Try to get tenant from header
        var tenantIdHeader = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        var apiKeyHeader = context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");

        Guid? tenantId = null;

        // Try header first
        if (!string.IsNullOrEmpty(tenantIdHeader) && Guid.TryParse(tenantIdHeader, out var headerTenantId))
        {
            tenantId = headerTenantId;
        }
        // Then try API key
        else if (!string.IsNullOrEmpty(apiKeyHeader))
        {
            var tenant = await tenantRepo.GetByApiKeyAsync(apiKeyHeader);
            if (tenant != null)
            {
                tenantId = tenant.Id;
            }
        }

        // If no tenant found, use default tenant for development
        if (!tenantId.HasValue)
        {
            // For development, use a default tenant
            var defaultTenantId = Guid.Parse("a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11");
            tenantId = defaultTenantId;
            _logger.LogWarning("No tenant header found, using default tenant: {TenantId}", defaultTenantId);
        }

        // Set tenant context
        tenantContext.TenantId = tenantId.Value;
        context.Items["TenantId"] = tenantId.Value.ToString();

        await _next(context);
    }
}