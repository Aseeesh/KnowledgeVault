using System.Diagnostics;

namespace KnowledgeVault.API.Middleware;

public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString("N")[..8];
        context.Response.Headers["X-Request-Id"] = requestId;

        try
        {
            await next(context);
            sw.Stop();

            logger.LogInformation(
                "{Method} {Path} {StatusCode} {ElapsedMs}ms [{RequestId}]",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                sw.ElapsedMilliseconds,
                requestId);
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex,
                "{Method} {Path} 500 {ElapsedMs}ms [{RequestId}]",
                context.Request.Method,
                context.Request.Path,
                sw.ElapsedMilliseconds,
                requestId);
            throw;
        }
    }
}
