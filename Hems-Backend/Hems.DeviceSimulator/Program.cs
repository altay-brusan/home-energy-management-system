using Hems.DeviceSimulator.Services;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

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

// Add Device Simulator Services
builder.Services.AddScoped<IDeviceSimulatorService, DeviceSimulatorService>();
builder.Services.AddScoped<IDataGeneratorService, DataGeneratorService>();

// Add Background Service for continuous simulation
builder.Services.AddHostedService<DeviceSimulationBackgroundService>();

var app = builder.Build();

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
