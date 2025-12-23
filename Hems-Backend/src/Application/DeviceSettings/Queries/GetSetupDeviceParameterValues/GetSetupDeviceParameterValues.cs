using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Mappings;
using MediatR;

namespace Hems_Backend.Application.DeviceSettings.Queries.GetSetupDeviceParameterValues;

public record GetSetupDeviceParameterValuesQuery : IRequest<SetupDeviceParameterValuesVm>
{
    public Guid SetupDeviceId { get; init; }
}

public class GetSetupDeviceParameterValuesQueryHandler : IRequestHandler<GetSetupDeviceParameterValuesQuery, SetupDeviceParameterValuesVm>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetSetupDeviceParameterValuesQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<SetupDeviceParameterValuesVm> Handle(GetSetupDeviceParameterValuesQuery request, CancellationToken cancellationToken)
    {
        var setupDeviceParameterValues = await _context.SetupDeviceParameterValues
            .Where(sdpv => sdpv.SetupDeviceId == request.SetupDeviceId)
            .OrderBy(sdpv => sdpv.SortOrder)
            .ProjectTo<SetupDeviceParameterValueDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return new SetupDeviceParameterValuesVm
        {
            SetupDeviceParameterValues = setupDeviceParameterValues
        };
    }
}
