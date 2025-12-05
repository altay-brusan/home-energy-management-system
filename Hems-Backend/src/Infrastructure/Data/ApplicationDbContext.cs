using System.Reflection;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Entities;
using Hems_Backend.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<TodoList> TodoLists => Set<TodoList>();

    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

    public DbSet<Site> Sites => Set<Site>();

    public DbSet<Setup> Setups => Set<Setup>();

    public DbSet<Device> Devices => Set<Device>();

    public DbSet<DeviceComponent> DeviceComponents => Set<DeviceComponent>();

    public DbSet<SetupDevice> SetupDevices => Set<SetupDevice>();

    public DbSet<ExternalDevice> ExternalDevices => Set<ExternalDevice>();

    public DbSet<Hems_Backend.Domain.Entities.DeviceData> DeviceData => Set<Hems_Backend.Domain.Entities.DeviceData>();
    
    public DbSet<DeviceEnergyData> DeviceEnergyData => Set<DeviceEnergyData>();

    public DbSet<DevicePowerData> DevicePowerData => Set<DevicePowerData>();

    public DbSet<DeviceWeatherData> DeviceWeatherData => Set<DeviceWeatherData>();

    public DbSet<DeviceHeartbeat> DeviceHeartbeats => Set<DeviceHeartbeat>();

    public DbSet<DeviceDisconnectLog> DeviceDisconnectLogs => Set<DeviceDisconnectLog>();

    public DbSet<Invitation> Invitations => Set<Invitation>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<UserSite> UserSites => Set<UserSite>();

    public DbSet<DeviceNotificationType> DeviceNotificationTypes => Set<DeviceNotificationType>();

    public DbSet<DeviceNotification> DeviceNotifications => Set<DeviceNotification>();

    public DbSet<Country> Countries => Set<Country>();

    public DbSet<City> Cities => Set<City>();

    public DbSet<ParameterGroup> ParameterGroups => Set<ParameterGroup>();

    public DbSet<Parameter> Parameters => Set<Parameter>();

    public DbSet<SetupDeviceParameterValue> SetupDeviceParameterValues => Set<SetupDeviceParameterValue>();

    public DbSet<ParameterValueSettingScheme> ParameterValueSettingSchemes => Set<ParameterValueSettingScheme>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
