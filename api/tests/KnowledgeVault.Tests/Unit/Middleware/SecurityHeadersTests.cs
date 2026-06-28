using FluentAssertions;
using KnowledgeVault.API.Middleware;
using Microsoft.AspNetCore.Http;

namespace KnowledgeVault.Tests.Unit.Middleware;

public class SecurityHeadersTests
{
    [Fact]
    public async Task SecurityHeaders_SetsAllHeaders()
    {
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        context.Response.Headers["X-Content-Type-Options"].ToString().Should().Be("nosniff");
        context.Response.Headers["X-Frame-Options"].ToString().Should().Be("DENY");
        context.Response.Headers["X-XSS-Protection"].ToString().Should().Be("1; mode=block");
        context.Response.Headers["Referrer-Policy"].ToString().Should().Be("strict-origin-when-cross-origin");
        context.Response.Headers["Content-Security-Policy"].Should().NotBeEmpty();
        context.Response.Headers["Permissions-Policy"].Should().NotBeEmpty();
    }

    [Fact]
    public async Task SecurityHeaders_HttpsOnly_SetsHSTS()
    {
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";

        await middleware.InvokeAsync(context);

        context.Response.Headers["Strict-Transport-Security"].Should().NotBeEmpty();
    }

    [Fact]
    public async Task SecurityHeaders_Http_NoHSTS()
    {
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();
        context.Request.Scheme = "http";

        await middleware.InvokeAsync(context);

        context.Response.Headers.ContainsKey("Strict-Transport-Security").Should().BeFalse();
    }
}
