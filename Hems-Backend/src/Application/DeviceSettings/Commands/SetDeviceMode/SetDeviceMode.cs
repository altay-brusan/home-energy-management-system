using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.DeviceSettings.Models;
using Hems_Backend.Domain.Entities;
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

    private const string ExportStrategyParameterName = "ExportStrategySelection";

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
        // Find SetupDevice by DeviceId (needed for all modes)
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
        string? exportStrategyValue = null;

        // For non-Custom modes: Update device via RPC
        if (request.Mode != DeviceMode.Custom)
        {
            // Get export strategy value for this mode
            exportStrategyValue = DeviceModeConfigurations.GetExportStrategyValue(request.Mode);
            if (exportStrategyValue == null)
            {
                return new SetDeviceModeResult
                {
                    Success = false,
                    ErrorMessage = $"No export strategy mapping found for mode: {request.Mode}"
                };
            }

            // Find the ExportStrategySelection parameter value for this device
            var exportStrategyParam = await _context.SetupDeviceParameterValues
                .Include(sdpv => sdpv.Parameter)
                .FirstOrDefaultAsync(
                    sdpv => sdpv.SetupDeviceId == setupDevice.Id &&
                            sdpv.Parameter != null &&
                            sdpv.Parameter.Name == ExportStrategyParameterName,
                    cancellationToken);

            if (exportStrategyParam?.Parameter == null)
            {
                return new SetDeviceModeResult
                {
                    Success = false,
                    ErrorMessage = $"Parameter '{ExportStrategyParameterName}' not found for this device"
                };
            }

            // Update device via RPC
            _logger.LogInformation(
                "Setting device {DeviceId} to mode {Mode} (ExportStrategy: {ExportStrategy})",
                device.Id, request.Mode, exportStrategyValue);

            var rpcResult = await _rpcExecutor.ExecuteParameterUpdateAsync(
                device.ThingsBoardDeviceId,
                ExportStrategyParameterName,
                exportStrategyValue,
                cancellationToken);

            if (!rpcResult.Success)
            {
                _logger.LogWarning(
                    "Failed to set mode {Mode} on device {DeviceId}: {Error}",
                    request.Mode, device.Id, rpcResult.ErrorMessage);

                return new SetDeviceModeResult
                {
                    Success = false,
                    ErrorMessage = rpcResult.ErrorMessage ?? "Failed to update device",
                    Mode = request.Mode
                };
            }

            // Update database value for export strategy parameter
            exportStrategyParam.Value = exportStrategyValue;
        }
        else
        {
            // Custom mode: Only update database, no RPC call
            _logger.LogInformation(
                "Setting device {DeviceId} to Custom mode (database only, no RPC)",
                device.Id);
        }

        // Deactivate all existing active modes for this device
        var existingActiveModes = await _context.DeviceModeHistory
            .Where(m => m.DeviceId == device.Id && m.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var existingMode in existingActiveModes)
        {
            existingMode.IsActive = false;
        }

        // Get mode description
        var modeInfo = DeviceModeConfigurations.GetAllModesWithDescriptions()
            .GetValueOrDefault(request.Mode);

        // Create new mode history record
        var modeHistory = new DeviceModeHistory
        {
            DeviceId = device.Id,
            Mode = (int)request.Mode,
            Description = modeInfo?.Name ?? request.Mode.ToString(),
            IsActive = true
        };

        _context.DeviceModeHistory.Add(modeHistory);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Successfully set device {DeviceId} to mode {Mode} and recorded in history",
            device.Id, request.Mode);

        return new SetDeviceModeResult
        {
            Success = true,
            Mode = request.Mode,
            ExportStrategy = exportStrategyValue
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
    public string? ExportStrategy { get; init; }
}
