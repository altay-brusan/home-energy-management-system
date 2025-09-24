using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Constants;
using Hems_Backend.Infrastructure.Data;
using Hems_Backend.Infrastructure.Data.Interceptors;
using Hems_Backend.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("Hems_BackendDb");
        Guard.Against.Null(connectionString, message: "Connection string 'Hems_BackendDb' not found.");

        builder.Services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        builder.Services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();

        builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
            options.UseNpgsql(connectionString);
        });


        builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        builder.Services.AddScoped<ApplicationDbContextInitialiser>();

        builder.Services.AddAuthentication()
            .AddBearerToken(IdentityConstants.BearerScheme);

        builder.Services.AddAuthorizationBuilder();

        builder.Services
            .AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddApiEndpoints();

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddTransient<IIdentityService, IdentityService>();
        builder.Services.AddScoped<IUserService, UserService>();

        builder.Services.AddAuthorization(options =>
        {
            // Admin Only Policies
            options.AddPolicy(Policies.CanPurge, policy => policy.RequireRole(Roles.Administrator));
            options.AddPolicy(Policies.AdminOnly, policy => policy.RequireRole(Roles.Administrator));
            options.AddPolicy(Policies.UserManagement, policy => policy.RequireRole(Roles.Administrator));
            
            // Device Management - Admin, MobilUser, TechnicalServiceUser
            options.AddPolicy(Policies.DeviceManagement, policy => 
                policy.RequireRole(Roles.Administrator, Roles.MobilUser, Roles.TechnicalServiceUser));
            
            // Device Read Only - Tüm roller
            options.AddPolicy(Policies.DeviceReadOnly, policy => 
                policy.RequireRole(Roles.Administrator, Roles.MobilUser, Roles.TechnicalServiceUser, Roles.DashboardUser));
            
            // Site Management - Admin, MobilUser, DashboardUser
            options.AddPolicy(Policies.SiteManagement, policy => 
                policy.RequireRole(Roles.Administrator, Roles.MobilUser, Roles.DashboardUser));
            
            // Setup Management - Admin, MobilUser
            options.AddPolicy(Policies.SetupManagement, policy => 
                policy.RequireRole(Roles.Administrator, Roles.MobilUser));
            
            // Dashboard Access - Admin, DashboardUser
            options.AddPolicy(Policies.DashboardAccess, policy => 
                policy.RequireRole(Roles.Administrator, Roles.DashboardUser));
        });
    }
}
