using Hems_Backend.Domain.Entities;

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

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
