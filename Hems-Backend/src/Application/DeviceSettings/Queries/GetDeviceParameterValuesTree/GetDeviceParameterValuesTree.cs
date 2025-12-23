using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.DeviceSettings.Queries.GetSetupDeviceParameterValuesTree;
using MediatR;

namespace Hems_Backend.Application.DeviceSettings.Queries.GetDeviceParameterValuesTree;

public record GetDeviceParameterValuesTreeQuery : IRequest<SetupDeviceParameterValuesTreeVm>
{
    public Guid DeviceId { get; init; }
}

public class GetDeviceParameterValuesTreeQueryHandler : IRequestHandler<GetDeviceParameterValuesTreeQuery, SetupDeviceParameterValuesTreeVm>
{
    private readonly IApplicationDbContext _context;
    private readonly IMediator _mediator;

    public GetDeviceParameterValuesTreeQueryHandler(IApplicationDbContext context, IMediator mediator)
    {
        _context = context;
        _mediator = mediator;
    }

    public async Task<SetupDeviceParameterValuesTreeVm> Handle(GetDeviceParameterValuesTreeQuery request, CancellationToken cancellationToken)
    {
        // Find the SetupDevice for this Device
        var setupDevice = await _context.SetupDevices
            .Where(sd => sd.DeviceId == request.DeviceId)
            .FirstOrDefaultAsync(cancellationToken);

        if (setupDevice == null)
        {
            // No setup device found, return empty result
            return new SetupDeviceParameterValuesTreeVm
            {
                SetupDeviceId = Guid.Empty,
                ParameterGroups = new List<ParameterGroupTreeDto>()
            };
        }

        // Delegate to existing query
        return await _mediator.Send(new GetSetupDeviceParameterValuesTreeQuery
        {
            SetupDeviceId = setupDevice.Id
        }, cancellationToken);
    }
}
