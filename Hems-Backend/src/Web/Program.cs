using Hems_Backend.Infrastructure.Data;
using Hems_Backend.Infrastructure.Identity;
using Hems_Backend.Web.Hubs;
using Hems_Backend.Web.Middleware;
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
// Comment out HTTPS redirection to allow HTTP connections from ThingsBoard
// app.UseHttpsRedirection();
app.UseStaticFiles();

// Use custom exception handler instead of developer exception page
app.UseExceptionHandler(options => {});

// Add rate limiting for device data endpoints
app.UseMiddleware<DeviceDataRateLimitMiddleware>();
// Register ThingsBoard API-key middleware BEFORE endpoints are mapped.
app.UseMiddleware<ApiKeyAuthenticationMiddleware>();

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
