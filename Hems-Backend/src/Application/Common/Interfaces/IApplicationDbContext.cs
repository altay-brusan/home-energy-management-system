using Hems_Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<TodoList> TodoLists { get; }

    DbSet<TodoItem> TodoItems { get; }

    DbSet<Site> Sites { get; }

    DbSet<Setup> Setups { get; }

    DbSet<Device> Devices { get; }

    DbSet<DeviceComponent> DeviceComponents { get; }

    DbSet<SetupDevice> SetupDevices { get; }

    DbSet<ExternalDevice> ExternalDevices { get; }

    DbSet<Hems_Backend.Domain.Entities.DeviceData> DeviceData { get; }
    
    DbSet<DeviceEnergyData> DeviceEnergyData { get; }

    DbSet<DevicePowerData> DevicePowerData { get; }

    DbSet<DeviceWeatherData> DeviceWeatherData { get; }

    DbSet<DeviceDisconnectLog> DeviceDisconnectLogs { get; }

    DbSet<Invitation> Invitations { get; }

    DbSet<Notification> Notifications { get; }

    DbSet<UserSite> UserSites { get; }

    DbSet<DeviceNotificationType> DeviceNotificationTypes { get; }

    DbSet<DeviceNotification> DeviceNotifications { get; }

    DbSet<Country> Countries { get; }

    DbSet<City> Cities { get; }

    DbSet<ParameterGroup> ParameterGroups { get; }

    DbSet<Parameter> Parameters { get; }

    DbSet<SetupDeviceParameterValue> SetupDeviceParameterValues { get; }

    DbSet<ParameterValueSettingScheme> ParameterValueSettingSchemes { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
