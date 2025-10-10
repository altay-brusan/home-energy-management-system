using Hems.DeviceSimulator.Services;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Hangfire;
using Hangfire.PostgreSql;
using Hangfire.Dashboard;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "HEMS Device Simulator API",
        Version = "v1",
        Description = "API for simulating HEMS devices and sending data to the main system"
    });
});

// Add HTTP Client
builder.Services.AddHttpClient();

// Add Database Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// Add Hangfire Services
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Add Hangfire Server
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = Environment.ProcessorCount * 5;
    options.ServerTimeout = TimeSpan.FromMinutes(4);
    options.ServerCheckInterval = TimeSpan.FromMinutes(1);
    options.SchedulePollingInterval = TimeSpan.FromSeconds(15);
});

// Add Device Simulator Services
builder.Services.AddScoped<IDataGeneratorService, DataGeneratorService>();
builder.Services.AddScoped<IDeviceSimulationJobService, DeviceSimulationJobService>();
builder.Services.AddScoped<IEnergyDataGeneratorService, EnergyDataGeneratorService>();
builder.Services.AddScoped<IEnergyDataJobService, EnergyDataJobService>();

var app = builder.Build();

// Configure Hangfire Dashboard
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() },
    DashboardTitle = "HEMS Device Simulator Jobs",
    DisplayStorageConnectionString = false
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "HEMS Device Simulator API v1");
        c.RoutePrefix = string.Empty; // Swagger UI'ı root'ta göster
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

// Hangfire Authorization Filter for Development
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // Development ortamında herkese açık
        // Production'da proper authentication eklenmeli
        return true;
    }
}
