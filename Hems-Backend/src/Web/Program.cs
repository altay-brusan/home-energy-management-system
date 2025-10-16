using Hems_Backend.Infrastructure.Data;
using Hems_Backend.Infrastructure.Identity;
using Hems_Backend.Web.Infrastructure;
using Hems_Backend.Web.Hubs;

var builder = WebApplication.CreateBuilder(args);

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

// Add rate limiting for device data endpoints
app.UseMiddleware<DeviceDataRateLimitMiddleware>();

app.UseSwaggerUi(settings =>
{
    settings.Path = "/api";
    settings.DocumentPath = "/api/specification.json";
});


app.UseMiddleware<GlobalExceptionMiddleware>();

app.Map("/", () => Results.Redirect("/api"));

app.MapEndpoints();

// Map SignalR Hub
app.MapHub<DeviceNotificationHub>("/deviceNotificationHub");

app.Run();

public partial class Program { }
