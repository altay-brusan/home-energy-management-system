using System.Diagnostics;

namespace Hems_Backend.Web.Infrastructure;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestPath = context.Request.Path;
        var requestMethod = context.Request.Method;
        var userAgent = context.Request.Headers.UserAgent.ToString();
        var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        _logger.LogInformation("🚀 Request started: {Method} {Path} from {RemoteIp} - UserAgent: {UserAgent}", 
            requestMethod, requestPath, remoteIp, userAgent);

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Request failed: {Method} {Path} - Error: {Error}", 
                requestMethod, requestPath, ex.Message);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            var statusCode = context.Response.StatusCode;
            
            if (statusCode == 404)
            {
                _logger.LogWarning("🔍 404 NOT FOUND: {Method} {Path} - Duration: {Duration}ms - This endpoint was not found!", 
                    requestMethod, requestPath, stopwatch.ElapsedMilliseconds);
            }
            else if (statusCode >= 400)
            {
                _logger.LogWarning("⚠️ HTTP {StatusCode}: {Method} {Path} - Duration: {Duration}ms", 
                    statusCode, requestMethod, requestPath, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogInformation("✅ Request completed: {Method} {Path} - Status: {StatusCode} - Duration: {Duration}ms", 
                    requestMethod, requestPath, statusCode, stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
