using AutoMapper;
using AutoMapper.QueryableExtensions;
using Hems_Backend.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.DeviceSettings.Queries.GetDeviceParameters;

public record GetDeviceParametersQuery : IRequest<DeviceParametersVm>
{
    public Guid SetupDeviceId { get; init; }
}

public class GetDeviceParametersQueryHandler : IRequestHandler<GetDeviceParametersQuery, DeviceParametersVm>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetDeviceParametersQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<DeviceParametersVm> Handle(GetDeviceParametersQuery request, CancellationToken cancellationToken)
    {
        var parameters = await _context.SetupDeviceParameterValues
            .Where(sdpv => sdpv.SetupDeviceId == request.SetupDeviceId)
            .Include(sdpv => sdpv.Parameter)
            .Include(sdpv => sdpv.ParameterGroup)
            .OrderBy(sdpv => sdpv.SortOrder)
            .ProjectTo<DeviceParameterDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return new DeviceParametersVm
        {
            SetupDeviceId = request.SetupDeviceId,
            Parameters = parameters
        };
    }
}

/// <summary>
/// View model for device parameters
/// </summary>
public record DeviceParametersVm
{
    public Guid SetupDeviceId { get; init; }
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
            .ForMember(d => d.ParameterGroupName, opt => opt.MapFrom(s => s.ParameterGroup != null ? s.ParameterGroup.Name : null));
    }
}
