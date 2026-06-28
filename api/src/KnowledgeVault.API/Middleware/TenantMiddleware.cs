using KnowledgeVault.Application.Common;
using KnowledgeVault.Core.Interfaces.Repositories;

namespace KnowledgeVault.API.Middleware;

public class TenantMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ITenantRepository tenantRepo, TenantContext tenantContext)
    {
        if (context.Request.Path.StartsWithSegments("/health") ||
            context.Request.Path.StartsWithSegments("/swagger"))
        {
            await next(context);
            return;
        }

        // Try X-Tenant-Id header first
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantIdHeader) &&
            Guid.TryParse(tenantIdHeader, out var tenantId))
        {
            var tenant = await tenantRepo.GetByIdAsync(tenantId);
            if (tenant is not null && tenant.IsActive)
            {
                tenantContext.TenantId = tenant.Id;
                tenantContext.TenantSlug = tenant.Slug;
                tenantContext.Tenant = tenant;
                await next(context);
                return;
            }
        }

        // Try API key from Authorization header
        var authHeader = context.Request.Headers.Authorization.ToString();
        if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var apiKey = authHeader["Bearer ".Length..].Trim();
            var tenant = await tenantRepo.GetByApiKeyAsync(apiKey);
            if (tenant is not null)
            {
                tenantContext.TenantId = tenant.Id;
                tenantContext.TenantSlug = tenant.Slug;
                tenantContext.Tenant = tenant;
                await next(context);
                return;
            }
        }

        // Try tenant ID from route
        if (context.Request.RouteValues.TryGetValue("tenantId", out var routeTenantId) &&
            Guid.TryParse(routeTenantId?.ToString(), out var routeId))
        {
            var tenant = await tenantRepo.GetByIdAsync(routeId);
            if (tenant is not null && tenant.IsActive)
            {
                tenantContext.TenantId = tenant.Id;
                tenantContext.TenantSlug = tenant.Slug;
                tenantContext.Tenant = tenant;
                await next(context);
                return;
            }
        }

        // For API endpoints that require tenant, return 401
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Tenant identification required. Provide X-Tenant-Id header or Bearer API key." });
            return;
        }

        await next(context);
    }
}
