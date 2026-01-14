namespace Hems_Backend.Web.Infrastructure;

/// <summary>
/// Middleware to extract the access_token from the query string for SignalR hub connections.
/// SignalR WebSocket connections cannot send the Authorization header after the initial handshake,
/// so the client sends the token via query string parameter 'access_token'.
/// This middleware extracts it and adds it to the Authorization header before authentication runs.
/// </summary>
public class SignalRQueryStringAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SignalRQueryStringAuthMiddleware> _logger;

    // Hub paths that need query string token extraction
    private static readonly string[] HubPaths = new[]
    {
        "/powerFlowHub",
        "/deviceNotificationHub",
        "/deviceStatusHub",
        "/deviceSettingsHub"
    };

    public SignalRQueryStringAuthMiddleware(RequestDelegate next, ILogger<SignalRQueryStringAuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value;

        // Check if this is a SignalR hub request
        if (path != null && IsSignalRHubPath(path))
        {
            // Check if there's no Authorization header but there is an access_token in query string
            if (!context.Request.Headers.ContainsKey("Authorization"))
            {
                var accessToken = context.Request.Query["access_token"].FirstOrDefault();

                if (!string.IsNullOrEmpty(accessToken))
                {
                    // Add the token to the Authorization header
                    context.Request.Headers["Authorization"] = $"Bearer {accessToken}";
                    _logger.LogDebug(
                        "SignalR auth middleware: Extracted access_token from query string for path {Path}",
                        path);
                }
                else
                {
                    _logger.LogDebug(
                        "SignalR auth middleware: No access_token in query string for path {Path}",
                        path);
                }
            }
        }

        await _next(context);
    }

    private static bool IsSignalRHubPath(string path)
    {
        foreach (var hubPath in HubPaths)
        {
            if (path.StartsWith(hubPath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}
