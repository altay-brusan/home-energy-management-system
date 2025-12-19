using Hems_Backend.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

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

    public UpdateDeviceParameterCommandHandler(
        IApplicationDbContext context,
        IDeviceRpcExecutorService rpcExecutor)
    {
        _context = context;
        _rpcExecutor = rpcExecutor;
    }

    public async Task<UpdateDeviceParameterResult> Handle(UpdateDeviceParameterCommand request, CancellationToken cancellationToken)
    {
        // Get the parameter value record
        var parameterValue = await _context.SetupDeviceParameterValues
            .Include(p => p.Parameter)
            .Include(p => p.SetupDevice)
            .FirstOrDefaultAsync(p =>
                p.SetupDeviceId == request.SetupDeviceId &&
                p.ParameterId == request.ParameterId,
                cancellationToken);

        if (parameterValue == null)
        {
            return new UpdateDeviceParameterResult
            {
                Success = false,
                ErrorMessage = "Parameter not found for this device"
            };
        }

        // Get the device ID from SetupDevice
        var deviceId = parameterValue.SetupDevice.DeviceId;

        // Execute RPC to update device FIRST
        var rpcResult = await _rpcExecutor.ExecuteParameterUpdateAsync(
            deviceId,
            parameterValue.Parameter.Name,
            request.Value,
            cancellationToken);

        if (!rpcResult.Success)
        {
            return new UpdateDeviceParameterResult
            {
                Success = false,
                ErrorMessage = $"Failed to update device: {rpcResult.ErrorMessage}",
                RpcMethod = rpcResult.RpcMethod
            };
        }

        // If RPC succeeded, update database
        parameterValue.Value = request.Value;
        await _context.SaveChangesAsync(cancellationToken);

        return new UpdateDeviceParameterResult
        {
            Success = true,
            ParameterId = request.ParameterId,
            ParameterName = parameterValue.Parameter.Name,
            NewValue = request.Value,
            RpcMethod = rpcResult.RpcMethod
        };
    }
}

public class UpdateDeviceParameterResult
{
    public bool Success { get; set; }
    public Guid? ParameterId { get; set; }
    public string? ParameterName { get; set; }
    public string? NewValue { get; set; }
    public string? RpcMethod { get; set; }
    public string? ErrorMessage { get; set; }
}
