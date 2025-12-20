using Hems_Backend.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Application.DeviceSettings.Commands.UpdateDeviceParameters;

public record UpdateDeviceParametersCommand : IRequest<UpdateDeviceParametersResult>
{
    public Guid DeviceId { get; init; }
    public List<ParameterUpdate> Parameters { get; init; } = new();
}

public record ParameterUpdate
{
    public Guid ParameterId { get; init; }
    public string Value { get; init; } = string.Empty;
}

public class UpdateDeviceParametersCommandHandler : IRequestHandler<UpdateDeviceParametersCommand, UpdateDeviceParametersResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IDeviceRpcExecutorService _rpcExecutor;
    private readonly ILogger<UpdateDeviceParametersCommandHandler> _logger;

    public UpdateDeviceParametersCommandHandler(
        IApplicationDbContext context,
        IDeviceRpcExecutorService rpcExecutor,
        ILogger<UpdateDeviceParametersCommandHandler> logger)
    {
        _context = context;
        _rpcExecutor = rpcExecutor;
        _logger = logger;
    }

    public async Task<UpdateDeviceParametersResult> Handle(UpdateDeviceParametersCommand request, CancellationToken cancellationToken)
    {
        if (request.Parameters.Count == 0)
        {
            return new UpdateDeviceParametersResult
            {
                Success = false,
                ErrorMessage = "No parameters provided"
            };
        }

        // Find SetupDevice by DeviceId
        var setupDevice = await _context.SetupDevices
            .Include(sd => sd.Device)
            .FirstOrDefaultAsync(sd => sd.DeviceId == request.DeviceId, cancellationToken);

        if (setupDevice?.Device == null)
        {
            return new UpdateDeviceParametersResult
            {
                Success = false,
                ErrorMessage = "Device not found"
            };
        }

        // Get all parameter values for this device
        var parameterIds = request.Parameters.Select(p => p.ParameterId).ToList();
        var parameterValues = await _context.SetupDeviceParameterValues
            .Include(sdpv => sdpv.Parameter)
            .Where(sdpv =>
                sdpv.SetupDeviceId == setupDevice.Id &&
                parameterIds.Contains(sdpv.ParameterId))
            .ToListAsync(cancellationToken);

        if (parameterValues.Count == 0)
        {
            return new UpdateDeviceParametersResult
            {
                Success = false,
                ErrorMessage = "No matching parameters found for this device"
            };
        }

        var device = setupDevice.Device;

        // Build parameter dictionary for RPC call (parameterName -> value)
        var parameterUpdates = new Dictionary<string, string>();
        var parameterValueMap = new Dictionary<string, Domain.Entities.SetupDeviceParameterValue>();

        foreach (var update in request.Parameters)
        {
            var paramValue = parameterValues.FirstOrDefault(pv => pv.ParameterId == update.ParameterId);
            if (paramValue?.Parameter != null)
            {
                parameterUpdates[paramValue.Parameter.Name] = update.Value;
                parameterValueMap[paramValue.Parameter.Name] = paramValue;
            }
        }

        // Step 1: Update device via RPC (batch optimized)
        var rpcResults = await _rpcExecutor.ExecuteParameterUpdatesAsync(
            device.ThingsBoardDeviceId,
            parameterUpdates,
            cancellationToken);

        // Track successes and failures
        var successfulUpdates = new List<ParameterUpdateStatus>();
        var failedUpdates = new List<ParameterUpdateStatus>();

        // Step 2: Update database for successful RPC calls only
        foreach (var (parameterName, rpcResult) in rpcResults)
        {
            if (parameterValueMap.TryGetValue(parameterName, out var paramValue))
            {
                if (rpcResult.Success)
                {
                    // Update database value
                    var newValue = parameterUpdates[parameterName];
                    paramValue.Value = newValue;

                    successfulUpdates.Add(new ParameterUpdateStatus
                    {
                        ParameterName = parameterName,
                        Success = true,
                        RpcMethod = rpcResult.RpcMethod,
                        NewValue = newValue
                    });

                    _logger.LogInformation(
                        "Successfully updated parameter {ParameterName} on device {DeviceId}",
                        parameterName,
                        device.Id);
                }
                else
                {
                    failedUpdates.Add(new ParameterUpdateStatus
                    {
                        ParameterName = parameterName,
                        Success = false,
                        ErrorMessage = rpcResult.ErrorMessage,
                        RpcMethod = rpcResult.RpcMethod
                    });

                    _logger.LogWarning(
                        "Failed to update parameter {ParameterName} on device {DeviceId}: {Error}",
                        parameterName,
                        device.Id,
                        rpcResult.ErrorMessage);
                }
            }
        }

        // Save all successful updates to database
        if (successfulUpdates.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        var overallSuccess = failedUpdates.Count == 0;
        var errorMessage = !overallSuccess
            ? $"{failedUpdates.Count} out of {rpcResults.Count} parameters failed to update"
            : null;

        return new UpdateDeviceParametersResult
        {
            Success = overallSuccess,
            ErrorMessage = errorMessage,
            SuccessfulUpdates = successfulUpdates,
            FailedUpdates = failedUpdates
        };
    }
}

/// <summary>
/// Result of batch parameter update
/// </summary>
public record UpdateDeviceParametersResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public List<ParameterUpdateStatus> SuccessfulUpdates { get; init; } = new();
    public List<ParameterUpdateStatus> FailedUpdates { get; init; } = new();
}

/// <summary>
/// Status of a single parameter update
/// </summary>
public record ParameterUpdateStatus
{
    public string ParameterName { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string? RpcMethod { get; init; }
    public string? NewValue { get; init; }
}
