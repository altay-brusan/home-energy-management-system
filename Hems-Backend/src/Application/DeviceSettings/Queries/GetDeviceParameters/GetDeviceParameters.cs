using AutoMapper;
using AutoMapper.QueryableExtensions;
using Hems_Backend.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.DeviceSettings.Queries.GetDeviceParameters;

public record GetDeviceParametersQuery : IRequest<DeviceParametersVm>
{
    public Guid DeviceId { get; init; }
}

public class GetDeviceParametersQueryHandler : IRequestHandler<GetDeviceParametersQuery, DeviceParametersVm>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IDemoDataService _demoDataService;

    public GetDeviceParametersQueryHandler(
        IApplicationDbContext context,
        IMapper mapper,
        IDemoDataService demoDataService)
    {
        _context = context;
        _mapper = mapper;
        _demoDataService = demoDataService;
    }

    public async Task<DeviceParametersVm> Handle(GetDeviceParametersQuery request, CancellationToken cancellationToken)
    {
        // Check if demo device - return simulated parameters
        var isDemoDevice = await _demoDataService.IsDemoDeviceAsync(request.DeviceId, cancellationToken);
        if (isDemoDevice)
        {
            return GetSimulatedDeviceParameters(request.DeviceId);
        }

        // Find SetupDevice by DeviceId
        var setupDevice = await _context.SetupDevices
            .FirstOrDefaultAsync(sd => sd.DeviceId == request.DeviceId, cancellationToken);

        if (setupDevice == null)
        {
            return new DeviceParametersVm
            {
                DeviceId = request.DeviceId,
                Parameters = new List<DeviceParameterDto>()
            };
        }

        var parameters = await _context.SetupDeviceParameterValues
            .Where(sdpv => sdpv.SetupDeviceId == setupDevice.Id)
            .Include(sdpv => sdpv.Parameter)
            .Include(sdpv => sdpv.ParameterGroup)
            .OrderBy(sdpv => sdpv.SortOrder)
            .ProjectTo<DeviceParameterDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return new DeviceParametersVm
        {
            DeviceId = request.DeviceId,
            Parameters = parameters
        };
    }

    /// <summary>
    /// Returns simulated device parameters for demo devices
    /// Represents typical hybrid inverter system configuration
    /// </summary>
    private static DeviceParametersVm GetSimulatedDeviceParameters(Guid deviceId)
    {
        var now = DateTimeOffset.UtcNow;
        var setupDeviceId = Guid.NewGuid();
        var gridGroupId = Guid.NewGuid();
        var batteryGroupId = Guid.NewGuid();
        var pvGroupId = Guid.NewGuid();

        var parameters = new List<DeviceParameterDto>
        {
            // Grid Settings
            new DeviceParameterDto
            {
                Id = Guid.NewGuid(),
                SetupDeviceId = setupDeviceId,
                ParameterId = Guid.NewGuid(),
                ParameterName = "GridMaxExportPower",
                ParameterDescription = "Maximum power allowed to export to grid",
                Value = "5000",
                Unit = "W",
                ElementType = "number",
                RoleType = "setting",
                ParameterGroupId = gridGroupId,
                ParameterGroupName = "Grid Settings",
                SortOrder = 1,
                LastModified = now.AddDays(-30)
            },
            new DeviceParameterDto
            {
                Id = Guid.NewGuid(),
                SetupDeviceId = setupDeviceId,
                ParameterId = Guid.NewGuid(),
                ParameterName = "GridVoltageHigh",
                ParameterDescription = "High voltage protection threshold",
                Value = "253",
                Unit = "V",
                ElementType = "number",
                RoleType = "setting",
                ParameterGroupId = gridGroupId,
                ParameterGroupName = "Grid Settings",
                SortOrder = 2,
                LastModified = now.AddDays(-30)
            },
            new DeviceParameterDto
            {
                Id = Guid.NewGuid(),
                SetupDeviceId = setupDeviceId,
                ParameterId = Guid.NewGuid(),
                ParameterName = "GridVoltageLow",
                ParameterDescription = "Low voltage protection threshold",
                Value = "207",
                Unit = "V",
                ElementType = "number",
                RoleType = "setting",
                ParameterGroupId = gridGroupId,
                ParameterGroupName = "Grid Settings",
                SortOrder = 3,
                LastModified = now.AddDays(-30)
            },
            // Battery Settings
            new DeviceParameterDto
            {
                Id = Guid.NewGuid(),
                SetupDeviceId = setupDeviceId,
                ParameterId = Guid.NewGuid(),
                ParameterName = "BatteryCapacity",
                ParameterDescription = "Total battery capacity",
                Value = "10240",
                Unit = "Wh",
                ElementType = "number",
                RoleType = "info",
                ParameterGroupId = batteryGroupId,
                ParameterGroupName = "Battery Settings",
                SortOrder = 10,
                LastModified = now.AddDays(-30)
            },
            new DeviceParameterDto
            {
                Id = Guid.NewGuid(),
                SetupDeviceId = setupDeviceId,
                ParameterId = Guid.NewGuid(),
                ParameterName = "BatteryMinSoc",
                ParameterDescription = "Minimum state of charge (discharge cutoff)",
                Value = "10",
                Unit = "%",
                ElementType = "number",
                RoleType = "setting",
                ParameterGroupId = batteryGroupId,
                ParameterGroupName = "Battery Settings",
                SortOrder = 11,
                LastModified = now.AddDays(-30)
            },
            new DeviceParameterDto
            {
                Id = Guid.NewGuid(),
                SetupDeviceId = setupDeviceId,
                ParameterId = Guid.NewGuid(),
                ParameterName = "BatteryMaxChargeCurrent",
                ParameterDescription = "Maximum battery charging current",
                Value = "50",
                Unit = "A",
                ElementType = "number",
                RoleType = "setting",
                ParameterGroupId = batteryGroupId,
                ParameterGroupName = "Battery Settings",
                SortOrder = 12,
                LastModified = now.AddDays(-30)
            },
            // PV Settings
            new DeviceParameterDto
            {
                Id = Guid.NewGuid(),
                SetupDeviceId = setupDeviceId,
                ParameterId = Guid.NewGuid(),
                ParameterName = "PvMaxPower",
                ParameterDescription = "Maximum PV array power",
                Value = "6000",
                Unit = "W",
                ElementType = "number",
                RoleType = "info",
                ParameterGroupId = pvGroupId,
                ParameterGroupName = "PV Settings",
                SortOrder = 20,
                LastModified = now.AddDays(-30)
            },
            new DeviceParameterDto
            {
                Id = Guid.NewGuid(),
                SetupDeviceId = setupDeviceId,
                ParameterId = Guid.NewGuid(),
                ParameterName = "PvMpptCount",
                ParameterDescription = "Number of MPPT channels",
                Value = "2",
                Unit = "",
                ElementType = "number",
                RoleType = "info",
                ParameterGroupId = pvGroupId,
                ParameterGroupName = "PV Settings",
                SortOrder = 21,
                LastModified = now.AddDays(-30)
            }
        };

        return new DeviceParametersVm
        {
            DeviceId = deviceId,
            Parameters = parameters
        };
    }
}

/// <summary>
/// View model for device parameters
/// </summary>
public record DeviceParametersVm
{
    public Guid DeviceId { get; init; }
    public List<DeviceParameterDto> Parameters { get; init; } = new();
}

/// <summary>
/// DTO for a single device parameter with metadata
/// </summary>
public record DeviceParameterDto
{
    public Guid Id { get; init; }
    public Guid SetupDeviceId { get; init; }
    public Guid ParameterId { get; init; }
    public string ParameterName { get; init; } = string.Empty;
    public string ParameterDescription { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public string ElementType { get; init; } = string.Empty;
    public string RoleType { get; init; } = string.Empty;
    public Guid? ParameterGroupId { get; init; }
    public string? ParameterGroupName { get; init; }
    public int SortOrder { get; init; }
    public DateTimeOffset? LastModified { get; init; }
}

/// <summary>
/// AutoMapper profile for device parameters
/// </summary>
public class DeviceParameterMappingProfile : Profile
{
    public DeviceParameterMappingProfile()
    {
        CreateMap<Domain.Entities.SetupDeviceParameterValue, DeviceParameterDto>()
            .ForMember(d => d.ParameterName, opt => opt.MapFrom(s => s.Parameter != null ? s.Parameter.Name : string.Empty))
            .ForMember(d => d.ParameterDescription, opt => opt.MapFrom(s => s.Parameter != null ? s.Parameter.Description : string.Empty))
            .ForMember(d => d.Unit, opt => opt.MapFrom(s => s.Parameter != null ? s.Parameter.Unit : string.Empty))
            .ForMember(d => d.ElementType, opt => opt.MapFrom(s => s.Parameter != null ? s.Parameter.ElementType : string.Empty))
            .ForMember(d => d.RoleType, opt => opt.MapFrom(s => s.Parameter != null ? s.Parameter.RoleType : string.Empty))
            .ForMember(d => d.ParameterGroupName, opt => opt.MapFrom(s => s.ParameterGroup != null ? s.ParameterGroup.Name : null));
    }
}
