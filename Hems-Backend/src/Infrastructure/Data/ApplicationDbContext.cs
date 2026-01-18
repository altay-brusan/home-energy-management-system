using System.Reflection;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Entities;
using Hems_Backend.Infrastructure.Identity;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext, IDataProtectionKeyContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

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

    public DbSet<EnergyPriceHistory> EnergyPriceHistory => Set<EnergyPriceHistory>();

    public DbSet<DeviceModeHistory> DeviceModeHistory => Set<DeviceModeHistory>();

    public DbSet<DeviceOperationHistory> DeviceOperationHistories => Set<DeviceOperationHistory>();

    public DbSet<DeviceSettingUpdateHistory> DeviceSettingUpdateHistories => Set<DeviceSettingUpdateHistory>();

    // Data Protection Keys for multi-pod environments
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
