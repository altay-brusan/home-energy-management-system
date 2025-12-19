using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.DeviceSettings.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Application.DeviceSettings.Commands.SetDeviceMode;

public record SetDeviceModeCommand : IRequest<SetDeviceModeResult>
{
    public Guid SetupDeviceId { get; init; }
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
        var modeConfig = DeviceModeConfigurations.GetModeConfiguration(request.Mode);
        if (modeConfig == null || !modeConfig.Any())
        {
            return new SetDeviceModeResult
            {
                Success = false,
                ErrorMessage = $"No configuration found for mode: {request.Mode}"
            };
        }

        // Get setup device with device info
        var setupDevice = await _context.SetupDevices
            .Include(sd => sd.Device)
            .FirstOrDefaultAsync(sd => sd.Id == request.SetupDeviceId, cancellationToken);

        if (setupDevice == null)
        {
            return new SetDeviceModeResult
            {
                Success = false,
                ErrorMessage = "Setup device not found"
            };
        }

        var deviceId = setupDevice.DeviceId;

        // Get all parameter values for this device
        var parameterValues = await _context.SetupDeviceParameterValues
            .Include(p => p.Parameter)
            .Where(p => p.SetupDeviceId == request.SetupDeviceId)
            .ToListAsync(cancellationToken);

        if (!parameterValues.Any())
        {
            return new SetDeviceModeResult
            {
                Success = false,
                ErrorMessage = "No parameters found for this device"
            };
        }

        // Build parameter dictionary for RPC
        // Only include parameters that exist in the device's parameter list
        var parameterUpdates = new Dictionary<string, string>();
        var parameterValueMap = new Dictionary<string, Domain.Entities.SetupDeviceParameterValue>();

        foreach (var kvp in modeConfig)
        {
            var paramValue = parameterValues.FirstOrDefault(p => p.Parameter.Name == kvp.Key);
            if (paramValue != null)
            {
                parameterUpdates[kvp.Key] = kvp.Value;
                parameterValueMap[kvp.Key] = paramValue;
            }
            else
            {
                _logger.LogWarning(
                    "Parameter {ParameterName} in mode {Mode} not found for device {DeviceId}",
                    kvp.Key, request.Mode, deviceId);
            }
        }

        if (!parameterUpdates.Any())
        {
            return new SetDeviceModeResult
            {
                Success = false,
                ErrorMessage = "No matching parameters found for this mode"
            };
        }

        // Execute RPC to update device FIRST (grouped by RPC method for efficiency)
        var rpcResults = await _rpcExecutor.ExecuteParameterUpdatesAsync(
            deviceId,
            parameterUpdates,
            cancellationToken);

        // Check if all RPC calls succeeded
        var failedRpcs = rpcResults.Where(r => !r.Success).ToList();
        if (failedRpcs.Any())
        {
            var errorMessages = string.Join("; ", failedRpcs.Select(r => $"{r.RpcMethod}: {r.ErrorMessage}"));
            return new SetDeviceModeResult
            {
                Success = false,
                Mode = request.Mode,
                ErrorMessage = $"Failed to apply mode to device: {errorMessages}",
                SuccessfulParameters = rpcResults
                    .Where(r => r.Success)
                    .SelectMany(r => r.UpdatedParameters)
                    .ToList(),
                FailedParameters = rpcResults
                    .Where(r => !r.Success)
                    .SelectMany(r => r.UpdatedParameters)
                    .ToList()
            };
        }

        // If all RPC calls succeeded, update database
        var updatedCount = 0;
        foreach (var kvp in parameterUpdates)
        {
            if (parameterValueMap.TryGetValue(kvp.Key, out var paramValue))
            {
                paramValue.Value = kvp.Value;
                updatedCount++;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Successfully applied mode {Mode} to device {DeviceId}. Updated {Count} parameters.",
            request.Mode, deviceId, updatedCount);

        return new SetDeviceModeResult
        {
            Success = true,
            Mode = request.Mode,
            UpdatedParametersCount = updatedCount,
            RpcMethods = rpcResults.Select(r => r.RpcMethod).Distinct().ToList()
        };
    }
}

public class SetDeviceModeResult
{
    public bool Success { get; set; }
    public DeviceMode? Mode { get; set; }
    public int UpdatedParametersCount { get; set; }
    public List<string> RpcMethods { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public List<string> SuccessfulParameters { get; set; } = new();
    public List<string> FailedParameters { get; set; } = new();
}
