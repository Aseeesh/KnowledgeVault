using System.Collections.Concurrent;
using KnowledgeVault.Application.Common;

namespace KnowledgeVault.API.Middleware;

public class RateLimitingMiddleware(RequestDelegate next)
{
    private static readonly ConcurrentDictionary<Guid, TenantRateLimit> Limits = new();

    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext)
    {
        if (tenantContext.TenantId == Guid.Empty)
        {
            await next(context);
            return;
        }

        var limit = Limits.GetOrAdd(tenantContext.TenantId, _ =>
            new TenantRateLimit(tenantContext.Tenant?.RateLimitPerMinute ?? 60));

        if (!limit.TryConsume())
        {
            context.Response.StatusCode = 429;
            context.Response.Headers["Retry-After"] = "60";
            await context.Response.WriteAsJsonAsync(new { error = "Rate limit exceeded" });
            return;
        }

        await next(context);
    }

    private class TenantRateLimit(int maxPerMinute)
    {
        private int _count;
        private DateTime _windowStart = DateTime.UtcNow;
        private readonly object _lock = new();

        public bool TryConsume()
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                if ((now - _windowStart).TotalMinutes >= 1)
                {
                    _windowStart = now;
                    _count = 0;
                }

                if (_count >= maxPerMinute)
                    return false;

                _count++;
                return true;
            }
        }
    }
}
