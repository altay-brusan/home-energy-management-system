using Hems_Backend.Infrastructure.Data;
using Hems_Backend.Infrastructure.Identity;
using Hems_Backend.Web.Hubs;
using Serilog;
using Serilog.Events;

// Npgsql DateTime handling - PostgreSQL UTC support için
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Configure Sentry
builder.WebHost.UseSentry(options =>
{
    // DSN comes from Kubernetes Secret via Sentry__Dsn environment variable
    // or from appsettings.json for local development
    options.SetBeforeSend((sentryEvent, hint) =>
    {
        // Remove sensitive data from requests
        if (sentryEvent.Request != null)
        {
            sentryEvent.Request.Cookies = null;
            sentryEvent.Request.Headers?.Remove("Authorization");
            sentryEvent.Request.Headers?.Remove("Cookie");
        }
        return sentryEvent;
    });
});

// Configure Serilog with Sentry integration
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/hems-backend-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        fileSizeLimitBytes: 10 * 1024 * 1024, // 10MB
        rollOnFileSizeLimit: true)
    .WriteTo.Sentry(o =>
    {
        o.MinimumBreadcrumbLevel = LogEventLevel.Information;
        o.MinimumEventLevel = LogEventLevel.Error;
    })
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
app.UseHttpsRedirection();
app.UseStaticFiles();

// Use Sentry request tracing
app.UseSentryTracing();

// Use custom exception handler instead of developer exception page
app.UseExceptionHandler(options => {});

// Add rate limiting for device data endpoints
app.UseMiddleware<DeviceDataRateLimitMiddleware>();

app.UseSwaggerUi(settings =>
{
    settings.Path = "/api";
    settings.DocumentPath = "/api/specification.json";
});

app.Map("/", () => Results.Redirect("/api"));

app.MapEndpoints();

// Map SignalR Hub
app.MapHub<DeviceNotificationHub>("/deviceNotificationHub");

app.Run();

public partial class Program { }
