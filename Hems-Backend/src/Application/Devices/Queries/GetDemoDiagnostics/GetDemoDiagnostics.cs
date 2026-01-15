using Hems_Backend.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Application.Devices.Queries.GetDemoDiagnostics;

/// <summary>
/// Response DTO for demo device diagnostics
/// </summary>
public record DemoDiagnosticsResult
{
    public WellKnownConstants WellKnownConstants { get; init; } = new();
    public DemoDeviceInfo? DemoDevice { get; init; }
    public SetupRelationshipInfo? SetupRelationship { get; init; }
    public string FrontendShouldUse { get; init; } = string.Empty;
    public StatusEndpointTestInfo? StatusEndpointTest { get; init; }
}

public record WellKnownConstants
{
    public string DemoEmail { get; init; } = string.Empty;
    public string DemoSerial { get; init; } = string.Empty;
    public Guid DemoThingsBoardId { get; init; }
}

public record DemoDeviceInfo
{
    public Guid DeviceId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string SerialNumber { get; init; } = string.Empty;
    public Guid? ThingsBoardDeviceId { get; init; }
    public bool IsDemoDeviceFlag { get; init; }
    public bool IsDeleted { get; init; }
    public bool? IsDemoDeviceAsyncResult { get; init; }
    public string Status { get; init; } = string.Empty;
}

public record SetupRelationshipInfo
{
    public Guid SetupId { get; init; }
    public string SetupName { get; init; } = string.Empty;
    public Guid SiteId { get; init; }
    public string SiteName { get; init; } = string.Empty;
    public string? SiteUserId { get; init; }
    public string Status { get; init; } = string.Empty;
}

public record StatusEndpointTestInfo
{
    public string Endpoint { get; init; } = string.Empty;
    public string ExpectedStatus { get; init; } = string.Empty;
    public string Note { get; init; } = string.Empty;
}

/// <summary>
/// Query to get demo device diagnostics
/// FOR TEST ONLY - DO NOT USE IN PRODUCTION
/// </summary>
public record GetDemoDiagnosticsQuery : IRequest<DemoDiagnosticsResult>;

public class GetDemoDiagnosticsQueryHandler : IRequestHandler<GetDemoDiagnosticsQuery, DemoDiagnosticsResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IDemoDataService _demoDataService;
    private readonly ILogger<GetDemoDiagnosticsQueryHandler> _logger;

    public GetDemoDiagnosticsQueryHandler(
        IApplicationDbContext context,
        IDemoDataService demoDataService,
        ILogger<GetDemoDiagnosticsQueryHandler> logger)
    {
        _context = context;
        _demoDataService = demoDataService;
        _logger = logger;
    }

    public async Task<DemoDiagnosticsResult> Handle(GetDemoDiagnosticsQuery request, CancellationToken cancellationToken)
    {
        // Get well-known demo constants
        var demoEmail = _demoDataService.GetDemoUserEmail();
        var demoSerial = _demoDataService.GetDemoDeviceSerialNumber();
        var demoThingsBoardId = _demoDataService.GetDemoThingsBoardDeviceId();

        // Check demo device by serial number
        var demoDevice = await _context.Devices
            .Where(d => d.SerialNumber == demoSerial)
            .Select(d => new
            {
                d.Id,
                d.Name,
                d.SerialNumber,
                d.ThingsBoardDeviceId,
                d.IsDemoDevice,
                d.IsDeleted
            })
            .FirstOrDefaultAsync(cancellationToken);

        // Check setup-device relationship
        var setupDevice = demoDevice != null
            ? await _context.SetupDevices
                .Where(sd => sd.DeviceId == demoDevice.Id)
                .Select(sd => new
                {
                    sd.SetupId,
                    SetupName = sd.Setup!.Name,
                    SiteId = sd.Setup.SiteId,
                    SiteName = sd.Setup.Site!.Name,
                    SiteUserId = sd.Setup.Site.UserId
                })
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        // Test IsDemoDeviceAsync method directly
        bool? isDemoDeviceResult = null;
        if (demoDevice != null)
        {
            isDemoDeviceResult = await _demoDataService.IsDemoDeviceAsync(demoDevice.Id, cancellationToken);
        }

        _logger.LogInformation("Demo diagnostics: Device={DeviceStatus}, Setup={SetupStatus}, IsDemoDeviceAsync={IsDemoResult}",
            demoDevice != null ? "Found" : "Missing",
            setupDevice != null ? "Found" : "Missing",
            isDemoDeviceResult);

        return new DemoDiagnosticsResult
        {
            WellKnownConstants = new WellKnownConstants
            {
                DemoEmail = demoEmail,
                DemoSerial = demoSerial,
                DemoThingsBoardId = demoThingsBoardId
            },
            DemoDevice = demoDevice != null
                ? new DemoDeviceInfo
                {
                    DeviceId = demoDevice.Id,
                    Name = demoDevice.Name ?? string.Empty,
                    SerialNumber = demoDevice.SerialNumber ?? string.Empty,
                    ThingsBoardDeviceId = demoDevice.ThingsBoardDeviceId,
                    IsDemoDeviceFlag = demoDevice.IsDemoDevice,
                    IsDeleted = demoDevice.IsDeleted,
                    IsDemoDeviceAsyncResult = isDemoDeviceResult,
                    Status = demoDevice.IsDemoDevice && !demoDevice.IsDeleted && isDemoDeviceResult == true
                        ? "OK"
                        : "WARNING: IsDemoDevice flag or IsDemoDeviceAsync check failed"
                }
                : null,
            SetupRelationship = setupDevice != null
                ? new SetupRelationshipInfo
                {
                    SetupId = setupDevice.SetupId,
                    SetupName = setupDevice.SetupName,
                    SiteId = setupDevice.SiteId,
                    SiteName = setupDevice.SiteName,
                    SiteUserId = setupDevice.SiteUserId,
                    Status = "OK"
                }
                : null,
            FrontendShouldUse = demoDevice != null
                ? $"Device ID (internal): {demoDevice.Id}"
                : "N/A - Demo device not found",
            StatusEndpointTest = demoDevice != null
                ? new StatusEndpointTestInfo
                {
                    Endpoint = $"/api/DeviceData/Status/{demoDevice.Id}",
                    ExpectedStatus = isDemoDeviceResult == true ? "OK" : "Check IsDemoDeviceAsync",
                    Note = "Call this endpoint to verify status returns OK for demo device"
                }
                : null
        };
    }
}
