using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.DeviceSettings.Queries.GetSetupDeviceParameterValuesTree;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Application.DeviceSettings.Queries.GetDeviceParameterValuesTree;

public record GetDeviceParameterValuesTreeQuery : IRequest<SetupDeviceParameterValuesTreeVm>
{
    public Guid DeviceId { get; init; }
}

public class GetDeviceParameterValuesTreeQueryHandler : IRequestHandler<GetDeviceParameterValuesTreeQuery, SetupDeviceParameterValuesTreeVm>
{
    private readonly IApplicationDbContext _context;
    private readonly IMediator _mediator;
    private readonly IOperationStateService _operationStateService;
    private readonly ILogger<GetDeviceParameterValuesTreeQueryHandler> _logger;

    /// <summary>
    /// Average time per parameter update in seconds (for estimation)
    /// </summary>
    private const int SecondsPerParameter = 2;

    public GetDeviceParameterValuesTreeQueryHandler(
        IApplicationDbContext context,
        IMediator mediator,
        IOperationStateService operationStateService,
        ILogger<GetDeviceParameterValuesTreeQueryHandler> logger)
    {
        _context = context;
        _mediator = mediator;
        _operationStateService = operationStateService;
        _logger = logger;
    }

    public async Task<SetupDeviceParameterValuesTreeVm> Handle(GetDeviceParameterValuesTreeQuery request, CancellationToken cancellationToken)
    {
        // Check if there's an ongoing update operation
        var activeOperation = _operationStateService.GetActiveOperationForDevice(request.DeviceId);

        if (activeOperation != null && !activeOperation.IsCompleted)
        {
            _logger.LogInformation(
                "Device {DeviceId} has active operation {OperationId} in progress. " +
                "Progress: {Current}/{Total} parameters. Returning busy status.",
                request.DeviceId, activeOperation.OperationId,
                activeOperation.CurrentIndex, activeOperation.TotalParameters);

            // Calculate progress and estimated time remaining
            var progressPercentage = activeOperation.TotalParameters > 0
                ? (int)((activeOperation.CurrentIndex * 100) / activeOperation.TotalParameters)
                : 0;

            var remainingParams = activeOperation.TotalParameters - activeOperation.CurrentIndex;
            var estimatedSecondsRemaining = remainingParams * SecondsPerParameter;

            // Return busy status - client should retry later
            return new SetupDeviceParameterValuesTreeVm
            {
                SetupDeviceId = Guid.Empty,
                ParameterGroups = new List<ParameterGroupTreeDto>(),
                IsDeviceBusy = true,
                ProgressPercentage = progressPercentage,
                EstimatedSecondsRemaining = estimatedSecondsRemaining
            };
        }

        // No active operation, proceed with fetching settings
        var setupDevice = await _context.SetupDevices
            .Where(sd => sd.DeviceId == request.DeviceId)
            .FirstOrDefaultAsync(cancellationToken);

        if (setupDevice == null)
        {
            return new SetupDeviceParameterValuesTreeVm
            {
                SetupDeviceId = Guid.Empty,
                ParameterGroups = new List<ParameterGroupTreeDto>()
            };
        }

        // Delegate to existing query
        var result = await _mediator.Send(new GetSetupDeviceParameterValuesTreeQuery
        {
            SetupDeviceId = setupDevice.Id
        }, cancellationToken);

        // Ensure IsDeviceBusy is false for successful responses
        result.IsDeviceBusy = false;
        return result;
    }
}
