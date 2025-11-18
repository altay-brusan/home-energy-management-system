using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Hems_Backend.Web.Middleware;

public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    public ApiKeyAuthenticationMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Accept either "/api/DeviceData" or "/DeviceData" route prefix
        var path = context.Request.Path;
        if (!path.StartsWithSegments("/api/DeviceData", StringComparison.OrdinalIgnoreCase) &&
            !path.StartsWithSegments("/DeviceData", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("X-API-Key", out StringValues extractedApiKeyValues) ||
            StringValues.IsNullOrEmpty(extractedApiKeyValues))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("API Key missing");
            return;
        }

        var extractedApiKey = extractedApiKeyValues.ToString();
        var configuredApiKey = _configuration["ApiKeys:ThingsBoard"];
        if (string.IsNullOrWhiteSpace(configuredApiKey) ||
            !string.Equals(extractedApiKey, configuredApiKey, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Invalid API Key");
            return;
        }

        await _next(context);
    }
}
