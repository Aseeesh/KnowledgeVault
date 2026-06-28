using FluentAssertions;
using KnowledgeVault.API.Middleware;
using KnowledgeVault.Application.Common;
using KnowledgeVault.Core.Entities;
using Microsoft.AspNetCore.Http;

namespace KnowledgeVault.Tests.Unit.Middleware;

public class RateLimitingTests
{
    [Fact]
    public async Task RateLimit_NoTenant_PassesThrough()
    {
        var middleware = new RateLimitingMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();
        var tenantContext = new TenantContext();

        await middleware.InvokeAsync(context, tenantContext);

        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task RateLimit_WithinLimit_PassesThrough()
    {
        var middleware = new RateLimitingMiddleware(_ => Task.CompletedTask);
        var tenantContext = new TenantContext
        {
            TenantId = Guid.NewGuid(),
            Tenant = new Tenant { Name = "Test", Slug = "test", RateLimitPerMinute = 100 }
        };

        for (int i = 0; i < 10; i++)
        {
            var context = new DefaultHttpContext();
            await middleware.InvokeAsync(context, tenantContext);
            context.Response.StatusCode.Should().Be(200);
        }
    }

    [Fact]
    public async Task RateLimit_ExceedsLimit_Returns429()
    {
        var tenantId = Guid.NewGuid();
        var middleware = new RateLimitingMiddleware(_ => Task.CompletedTask);
        var tenantContext = new TenantContext
        {
            TenantId = tenantId,
            Tenant = new Tenant { Name = "Test", Slug = "test", RateLimitPerMinute = 3 }
        };

        for (int i = 0; i < 3; i++)
        {
            var ctx = new DefaultHttpContext();
            ctx.Response.Body = new MemoryStream();
            await middleware.InvokeAsync(ctx, tenantContext);
        }

        var blocked = new DefaultHttpContext();
        blocked.Response.Body = new MemoryStream();
        await middleware.InvokeAsync(blocked, tenantContext);

        blocked.Response.StatusCode.Should().Be(429);
    }
}
