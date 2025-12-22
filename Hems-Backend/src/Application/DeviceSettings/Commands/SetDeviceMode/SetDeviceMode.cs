using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.DeviceSettings.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Application.DeviceSettings.Commands.SetDeviceMode;

public record SetDeviceModeCommand : IRequest<SetDeviceModeResult>
{
    public Guid DeviceId { get; init; }
    public DeviceMode Mode { get; init; }
}

public class SetDeviceModeCommandHandler : IRequestHandler<SetDeviceModeCommand, SetDeviceModeResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IDeviceRpcExecutorService _rpcExecutor;
    private readonly ILogger<SetDeviceModeCommandHandler> _logger;

    public SetDeviceModeCommandHandler(
        IApplicationDbContext context,
        IDeviceRpcExecutorService rpcExecutor,
        ILogger<SetDeviceModeCommandHandler> logger)
    {
        _context = context;
        _rpcExecutor = rpcExecutor;
        _logger = logger;
    }

    public async Task<SetDeviceModeResult> Handle(SetDeviceModeCommand request, CancellationToken cancellationToken)
    {
        // Get mode configuration
        var modeConfig = DeviceModeConfigurations.GetConfiguration(request.Mode);
        if (modeConfig == null)
        {
            return new SetDeviceModeResult
            {
                Success = false,
                ErrorMessage = $"No configuration found for mode: {request.Mode}"
            };
        }

        // Find SetupDevice by DeviceId
        var setupDevice = await _context.SetupDevices
            .Include(sd => sd.Device)
            .FirstOrDefaultAsync(sd => sd.DeviceId == request.DeviceId, cancellationToken);

        if (setupDevice?.Device == null)
        {
            return new SetDeviceModeResult
            {
                Success = false,
                ErrorMessage = "Device not found"
            };
        }

        var device = setupDevice.Device;

        // Get all parameter values for this device
        var allParameterValues = await _context.SetupDeviceParameterValues
            .Include(sdpv => sdpv.Parameter)
            .Where(sdpv => sdpv.SetupDeviceId == setupDevice.Id)
            .ToListAsync(cancellationToken);

        // Build parameter updates map (parameterName -> value)
        var parameterUpdates = new Dictionary<string, string>();
        var parameterValueMap = new Dictionary<string, Domain.Entities.SetupDeviceParameterValue>();

        foreach (var (paramName, value) in modeConfig)
        {
            var paramValue = allParameterValues.FirstOrDefault(pv => pv.Parameter?.Name == paramName);
            if (paramValue?.Parameter != null)
            {
                parameterUpdates[paramValue.Parameter.Name] = value;
                parameterValueMap[paramValue.Parameter.Name] = paramValue;
            }
        }

        if (parameterUpdates.Count == 0)
        {
            return new SetDeviceModeResult
            {
                Success = false,
                ErrorMessage = "No matching parameters found for this mode configuration"
            };
        }

        // Step 1: Update device via RPC (batch optimized)
        var rpcResults = await _rpcExecutor.ExecuteParameterUpdatesAsync(
            device.ThingsBoardDeviceId,
            parameterUpdates,
            cancellationToken);

        // Track successes and failures
        var successfulParameters = new List<string>();
        var failedParameters = new List<ModeParameterFailure>();

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
                    successfulParameters.Add(parameterName);

                    _logger.LogInformation(
                        "Mode {Mode}: Successfully updated parameter {ParameterName} on device {DeviceId}",
                        request.Mode,
                        parameterName,
                        device.Id);
                }
                else
                {
                    failedParameters.Add(new ModeParameterFailure
                    {
                        ParameterName = parameterName,
                        ErrorMessage = rpcResult.ErrorMessage ?? "Unknown error"
                    });

                    _logger.LogWarning(
                        "Mode {Mode}: Failed to update parameter {ParameterName} on device {DeviceId}: {Error}",
                        request.Mode,
                        parameterName,
                        device.Id,
                        rpcResult.ErrorMessage);
                }
            }
        }

        // Save all successful updates to database
        if (successfulParameters.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        var overallSuccess = failedParameters.Count == 0;
        var errorMessage = !overallSuccess
            ? $"Mode partially applied: {failedParameters.Count} out of {rpcResults.Count} parameters failed"
            : null;

        if (overallSuccess)
        {
            _logger.LogInformation(
                "Successfully applied mode {Mode} to device {DeviceId}",
                request.Mode,
                device.Id);
        }

        return new SetDeviceModeResult
        {
            Success = overallSuccess,
            ErrorMessage = errorMessage,
            Mode = request.Mode,
            SuccessfulParameters = successfulParameters,
            FailedParameters = failedParameters
        };
    }
}

/// <summary>
/// Result of setting device mode
/// </summary>
public record SetDeviceModeResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public DeviceMode Mode { get; init; }
    public List<string> SuccessfulParameters { get; init; } = new();
    public List<ModeParameterFailure> FailedParameters { get; init; } = new();
}

/// <summary>
/// Information about a parameter that failed during mode application
/// </summary>
public record ModeParameterFailure
{
    public string ParameterName { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
}
