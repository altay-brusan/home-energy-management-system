using Hems_Backend.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Application.DeviceSettings.Commands.UpdateDeviceParameter;

public record UpdateDeviceParameterCommand : IRequest<UpdateDeviceParameterResult>
{
    public Guid SetupDeviceId { get; init; }
    public Guid ParameterId { get; init; }
    public string Value { get; init; } = string.Empty;
}

public class UpdateDeviceParameterCommandHandler : IRequestHandler<UpdateDeviceParameterCommand, UpdateDeviceParameterResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IDeviceRpcExecutorService _rpcExecutor;
    private readonly ILogger<UpdateDeviceParameterCommandHandler> _logger;

    public UpdateDeviceParameterCommandHandler(
        IApplicationDbContext context,
        IDeviceRpcExecutorService rpcExecutor,
        ILogger<UpdateDeviceParameterCommandHandler> logger)
    {
        _context = context;
        _rpcExecutor = rpcExecutor;
        _logger = logger;
    }

    public async Task<UpdateDeviceParameterResult> Handle(UpdateDeviceParameterCommand request, CancellationToken cancellationToken)
    {
        // Get the parameter value record
        var parameterValue = await _context.SetupDeviceParameterValues
            .Include(sdpv => sdpv.Parameter)
            .Include(sdpv => sdpv.SetupDevice)
                .ThenInclude(sd => sd!.Device)
            .FirstOrDefaultAsync(sdpv =>
                sdpv.SetupDeviceId == request.SetupDeviceId &&
                sdpv.ParameterId == request.ParameterId,
                cancellationToken);

        if (parameterValue == null)
        {
            return new UpdateDeviceParameterResult
            {
                Success = false,
                ErrorMessage = "Parameter value not found for this device"
            };
        }

        Guard.Against.Null(parameterValue.SetupDevice?.Device);
        Guard.Against.Null(parameterValue.Parameter);

        var device = parameterValue.SetupDevice.Device;
        var parameterName = parameterValue.Parameter.Name;

        // Step 1: Update device via RPC first
        var rpcResult = await _rpcExecutor.ExecuteParameterUpdateAsync(
            device.ThingsBoardDeviceId,
            parameterName,
            request.Value,
            cancellationToken);

        if (!rpcResult.Success)
        {
            _logger.LogWarning(
                "Failed to update parameter {ParameterName} on device {DeviceId}: {Error}",
                parameterName,
                device.Id,
                rpcResult.ErrorMessage);

            return new UpdateDeviceParameterResult
            {
                Success = false,
                ErrorMessage = rpcResult.ErrorMessage ?? "Failed to update device parameter",
                ParameterName = parameterName,
                RpcMethod = rpcResult.RpcMethod
            };
        }

        // Step 2: If RPC successful, update database
        parameterValue.Value = request.Value;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Successfully updated parameter {ParameterName} on device {DeviceId} to value {Value}",
            parameterName,
            device.Id,
            request.Value);

        return new UpdateDeviceParameterResult
        {
            Success = true,
            ParameterName = parameterName,
            RpcMethod = rpcResult.RpcMethod,
            NewValue = request.Value
        };
    }
}

/// <summary>
/// Result of updating a device parameter
/// </summary>
public record UpdateDeviceParameterResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ParameterName { get; init; }
    public string? RpcMethod { get; init; }
    public string? NewValue { get; init; }
}
