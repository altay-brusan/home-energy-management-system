using System.Collections.Concurrent;
using System.Net;

namespace Hems_Backend.Web.Infrastructure;

public class DeviceDataRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DeviceDataRateLimitMiddleware> _logger;
    private readonly ConcurrentDictionary<string, RateLimitInfo> _rateLimitStore = new();
    private readonly TimeSpan _windowSize = TimeSpan.FromMinutes(1);
    private readonly int _maxRequestsPerMinute = 100; // Max 100 requests per minute per IP

    public DeviceDataRateLimitMiddleware(RequestDelegate next, ILogger<DeviceDataRateLimitMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api/DeviceData"))
        {
            var clientIp = GetClientIpAddress(context);
            var now = DateTime.UtcNow;

            var rateLimitInfo = _rateLimitStore.AddOrUpdate(clientIp,
                new RateLimitInfo { RequestCount = 1, WindowStart = now },
                (key, existing) =>
                {
                    if (now - existing.WindowStart > _windowSize)
                    {
                        return new RateLimitInfo { RequestCount = 1, WindowStart = now };
                    }
                    return new RateLimitInfo 
                    { 
                        RequestCount = existing.RequestCount + 1, 
                        WindowStart = existing.WindowStart 
                    };
                });

            if (rateLimitInfo.RequestCount > _maxRequestsPerMinute)
            {
                _logger.LogWarning("Rate limit exceeded for IP {ClientIp}. Requests: {RequestCount}", 
                    clientIp, rateLimitInfo.RequestCount);

                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.Response.Headers["Retry-After"] = "60";
                await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
                return;
            }

            // Add rate limit headers
            context.Response.Headers["X-RateLimit-Limit"] = _maxRequestsPerMinute.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = 
                (_maxRequestsPerMinute - rateLimitInfo.RequestCount).ToString();
            context.Response.Headers["X-RateLimit-Reset"] = 
                (rateLimitInfo.WindowStart.Add(_windowSize) - DateTime.UnixEpoch).TotalSeconds.ToString();
        }

        await _next(context);
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private class RateLimitInfo
    {
        public int RequestCount { get; set; }
        public DateTime WindowStart { get; set; }
    }
}

