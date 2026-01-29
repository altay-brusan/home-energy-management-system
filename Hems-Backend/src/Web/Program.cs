using Hangfire;
using Hangfire.Dashboard;
using Hangfire.Dashboard.BasicAuthorization;
using Hems_Backend.Infrastructure.Data;
using Hems_Backend.Infrastructure.Identity;
using Hems_Backend.Infrastructure.Hubs;
using Hems_Backend.Web.Infrastructure;
using Serilog;

// Npgsql DateTime handling - PostgreSQL UTC support için
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel for long-running requests (device parameter updates can take 2-3 minutes)
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(5);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(5);
});

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/hems-backend-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        fileSizeLimitBytes: 10 * 1024 * 1024, // 10MB
        rollOnFileSizeLimit: true)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.AddKeyVaultIfConfigured();
builder.AddApplicationServices();
builder.AddInfrastructureServices();
builder.AddWebServices();

// Add HTTP Client Factory
builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    await app.InitialiseDatabaseAsync();
}
else
{
    await app.InitialiseDatabaseAsync();
    app.UseHsts();
}

app.UseHealthChecks("/health");

// Configure Hangfire Dashboard with Basic Authentication
var hangfireUsername = builder.Configuration["Hangfire:Dashboard:Username"] ?? "admin";
var hangfirePassword = builder.Configuration["Hangfire:Dashboard:Password"] ?? "admin";

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    DashboardTitle = "HEMS Energy Collection Jobs",
    DisplayStorageConnectionString = false,
    Authorization = app.Environment.IsDevelopment()
        ? Array.Empty<IDashboardAuthorizationFilter>()
        : new IDashboardAuthorizationFilter[]
        {
            new BasicAuthAuthorizationFilter(new BasicAuthAuthorizationFilterOptions
            {
                RequireSsl = false, // Set to true in production with HTTPS
                SslRedirect = false,
                LoginCaseSensitive = true,
                Users = new[]
                {
                    new BasicAuthAuthorizationUser
                    {
                        Login = hangfireUsername,
                        PasswordClear = hangfirePassword
                    }
                }
            })
        }
});

// Allow HTTP for ThingsBoard telemetry endpoints, require HTTPS for all other endpoints
app.Use(async (context, next) =>
{
    // Skip HTTPS redirection for DeviceData endpoints (ThingsBoard integration)
    if (!context.Request.Path.StartsWithSegments("/api/DeviceData"))
    {
        if (!context.Request.IsHttps && !app.Environment.IsDevelopment() && !app.Environment.IsStaging())
        {
            var httpsUrl = $"https://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}";
            context.Response.Redirect(httpsUrl, permanent: true);
            return;
        }
    }
    await next();
});

// Configure static files with proper content type for apple-app-site-association
app.UseStaticFiles(new StaticFileOptions
{
    ServeUnknownFileTypes = true,
    OnPrepareResponse = ctx =>
    {
        // Apple App Site Association file must be served as application/json
        if (ctx.File.Name == "apple-app-site-association")
        {
            ctx.Context.Response.ContentType = "application/json";
        }
    }
});

// Use custom exception handler to convert exceptions to structured API responses
app.UseMiddleware<GlobalExceptionMiddleware>();

// Extract access_token from query string for SignalR WebSocket connections
// MUST be before UseAuthentication to inject token into Authorization header
app.UseMiddleware<SignalRQueryStringAuthMiddleware>();

// Authentication and Authorization
app.UseAuthentication();
app.UseAuthorization();

// Add rate limiting for device data endpoints
app.UseMiddleware<DeviceDataRateLimitMiddleware>();

app.UseSwaggerUi(settings =>
{
    settings.Path = "/api";
    settings.DocumentPath = "/api/specification.json";
});

app.Map("/", () => Results.Redirect("/api"));

app.MapEndpoints();

// Map SignalR Hubs
app.MapHub<DeviceNotificationHub>("/deviceNotificationHub");
app.MapHub<PowerFlowHub>("/powerFlowHub");
app.MapHub<DeviceStatusHub>("/deviceStatusHub");
app.MapHub<DeviceSettingsHub>("/deviceSettingsHub");
app.MapHub<AlmNotificationHub>("/almNotificationHub");
app.Run();

public partial class Program { }
