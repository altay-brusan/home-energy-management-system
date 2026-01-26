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
    private readonly IDemoSimulationDataService _demoSimulationService;

    public GetLifetimeEnergyQueryHandler(
        IApplicationDbContext context,
        ISender sender,
        IDeviceMappingService deviceMapping,
        IDemoDataService demoDataService,
        IDemoSimulationDataService demoSimulationService)
    {
        _context = context;
        _sender = sender;
        _deviceMapping = deviceMapping;
        _demoDataService = demoDataService;
        _demoSimulationService = demoSimulationService;
    }

    public async Task<LifetimeEnergyDto?> Handle(GetLifetimeEnergyQuery request, CancellationToken cancellationToken)
    {
        // Check device access
        var hasAccess = await _sender.Send(new CheckDeviceAccessQuery { DeviceId = request.DeviceId }, cancellationToken);
        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("Device access denied");
        }

        // Check if demo device - return simulated lifetime energy from centralized service
        var isDemoDevice = await _demoDataService.IsDemoDeviceAsync(request.DeviceId, cancellationToken);
        if (isDemoDevice)
        {
            var demoData = _demoSimulationService.GetDemoLifetimeEnergy();
            return new LifetimeEnergyDto
            {
                Timestamp = demoData.Timestamp,
                GridImport = demoData.GridImport,
                GridExport = demoData.GridExport
            };
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
}
