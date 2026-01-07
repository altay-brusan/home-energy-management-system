using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.Devices.Queries.CheckDeviceAccess;
using Hems_Backend.Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Dashboard.Queries.GetLifetimeEnergy;

public record GetLifetimeEnergyQuery : IRequest<LifetimeEnergyDto?>
{
    public Guid DeviceId { get; init; }
}

public class GetLifetimeEnergyQueryHandler : IRequestHandler<GetLifetimeEnergyQuery, LifetimeEnergyDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ISender _sender;
    private readonly IDeviceMappingService _deviceMapping;
    private readonly IDemoDataService _demoDataService;

    public GetLifetimeEnergyQueryHandler(
        IApplicationDbContext context,
        ISender sender,
        IDeviceMappingService deviceMapping,
        IDemoDataService demoDataService)
    {
        _context = context;
        _sender = sender;
        _deviceMapping = deviceMapping;
        _demoDataService = demoDataService;
    }

    public async Task<LifetimeEnergyDto?> Handle(GetLifetimeEnergyQuery request, CancellationToken cancellationToken)
    {
        // Check device access
        var hasAccess = await _sender.Send(new CheckDeviceAccessQuery { DeviceId = request.DeviceId }, cancellationToken);
        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("Device access denied");
        }

        // Check if demo device - return simulated lifetime energy
        var isDemoDevice = await _demoDataService.IsDemoDeviceAsync(request.DeviceId, cancellationToken);
        if (isDemoDevice)
        {
            return GetSimulatedLifetimeEnergy();
        }

        // Map local DeviceId to ThingsBoardDeviceId
        var thingsBoardDeviceId = await _deviceMapping.GetThingsBoardDeviceIdAsync(request.DeviceId, cancellationToken);
        if (thingsBoardDeviceId == null)
        {
            throw new ArgumentException($"Device with ID {request.DeviceId} not found or has no ThingsBoard mapping.");
        }

        // Get the most recent LifetimeEnergy record
        var lifetimeRecord = await _context.DeviceEnergyData
            .Where(x => x.ThingsBoardDeviceId == thingsBoardDeviceId.Value &&
                       x.DataType == DataTypes.LifetimeEnergy)
            .OrderByDescending(x => x.DeviceLocalTime)
            .FirstOrDefaultAsync(cancellationToken);

        if (lifetimeRecord == null)
        {
            // No lifetime data available
            return null;
        }

        return new LifetimeEnergyDto
        {
            Timestamp = lifetimeRecord.DeviceLocalTime,
            GridImport = Math.Round(lifetimeRecord.GridImportEnergyKWh ?? 0, 2),
            GridExport = Math.Round(lifetimeRecord.GridExportEnergyKWh ?? 0, 2)
        };
    }

    /// <summary>
    /// Returns simulated lifetime energy for demo devices
    /// Simulates a system that has been running for about 2 years
    /// </summary>
    private static LifetimeEnergyDto GetSimulatedLifetimeEnergy()
    {
        var now = DateTime.UtcNow;

        // Simulate a system installed ~2 years ago
        // Average daily production: ~25 kWh, consumption: ~18 kWh
        // Days since "installation": ~730 days (2 years)
        var daysInstalled = 730 + now.DayOfYear;

        // Total lifetime energy calculations
        // Grid Import: Energy bought from grid when solar insufficient
        // Grid Export: Excess solar energy sold to grid
        var baseGridImport = 3500.0; // kWh over 2 years (low due to good solar)
        var baseGridExport = 8200.0; // kWh exported over 2 years

        // Add some variation based on current date
        var dayVariation = now.DayOfYear * 2.5; // ~2.5 kWh variation per day

        return new LifetimeEnergyDto
        {
            Timestamp = now,
            GridImport = Math.Round(baseGridImport + dayVariation * 0.4, 2),
            GridExport = Math.Round(baseGridExport + dayVariation * 0.8, 2)
        };
    }
}
