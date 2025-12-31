using Hems_Backend.Infrastructure.Data;
using Hems_Backend.Infrastructure.Identity;
using Hems_Backend.Web.Hubs;
using Serilog;

// Npgsql DateTime handling - PostgreSQL UTC support için
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

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

app.UseStaticFiles();

// Use custom exception handler instead of developer exception page
app.UseExceptionHandler(options => { });

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
app.MapHub<Hems_Backend.Infrastructure.Hubs.DeviceStatusHub>("/deviceStatusHub");
app.MapHub<DeviceSettingsHub>("/deviceSettingsHub");
app.Run();

public partial class Program { }
