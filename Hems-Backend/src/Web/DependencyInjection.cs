using Azure.Identity;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Security;
using Hems_Backend.Domain.Constants;
using Hems_Backend.Infrastructure.Data;
using Hems_Backend.Web.Services;
using Hems_Backend.Web.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

using NSwag;
using NSwag.Generation.Processors.Security;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static void AddWebServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<IUser, CurrentUser>();

        // Add HttpClient for DeviceSimulator communication
        builder.Services.AddHttpClient();

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>();

        builder.Services.AddExceptionHandler<CustomExceptionHandler>();
        
        // Configure exception handler options
        builder.Services.Configure<Microsoft.AspNetCore.Builder.ExceptionHandlerOptions>(options =>
        {
            options.AllowStatusCode404Response = true;
        });

        // Add SignalR
        builder.Services.AddSignalR();

        // PowerFlow service (uses centralized ThingsBoard connector for RPC)
        builder.Services.AddScoped<IPowerFlowService, PowerFlowService>();

        // Add Device Notification Service
        builder.Services.AddScoped<IDeviceNotificationService, DeviceNotificationService>();
  
        // Add Authorization Policies
        //builder.Services.AddAuthorization(options =>
        //{
        //    options.AddPolicy(Policies.DeviceOnly, policy =>
        //    {
        //        policy.Requirements.Add(new DeviceOnlyRequirement());
        //    });
        //});

        //builder.Services.AddScoped<IAuthorizationHandler, DeviceOnlyAuthorizationHandler>();

        // Customise default API behaviour
        builder.Services.Configure<ApiBehaviorOptions>(options =>
            options.SuppressModelStateInvalidFilter = true);

        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddOpenApiDocument((configure, sp) =>
        {
            configure.Title = "Hems_Backend API";

            // Add JWT
            configure.AddSecurity("JWT", Enumerable.Empty<string>(), new OpenApiSecurityScheme
            {
                Type = OpenApiSecuritySchemeType.ApiKey,
                Name = "Authorization",
                In = OpenApiSecurityApiKeyLocation.Header,
                Description = "Type into the textbox: Bearer {your JWT token}."
            });

            configure.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("JWT"));
        });
    }

    public static void AddKeyVaultIfConfigured(this IHostApplicationBuilder builder)
    {
        var keyVaultUri = builder.Configuration["AZURE_KEY_VAULT_ENDPOINT"];
        if (!string.IsNullOrWhiteSpace(keyVaultUri))
        {
            builder.Configuration.AddAzureKeyVault(
                new Uri(keyVaultUri),
                new DefaultAzureCredential());
        }
    }
}
