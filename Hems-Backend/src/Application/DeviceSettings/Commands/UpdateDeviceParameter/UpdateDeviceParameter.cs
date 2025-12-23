using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Application.DeviceSettings.Commands.UpdateDeviceParameter;

public record UpdateDeviceParameterCommand : IRequest<UpdateDeviceParameterResult>
{
    public Guid DeviceId { get; init; }
    public Guid ParameterId { get; init; }
    public string Value { get; init; } = string.Empty;
}

public class UpdateDeviceParameterCommandHandler : IRequestHandler<UpdateDeviceParameterCommand, UpdateDeviceParameterResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IDeviceRpcExecutorService _rpcExecutor;
    private readonly IIdentityService _identityService;
    private readonly IUser _currentUser;
    private readonly ILogger<UpdateDeviceParameterCommandHandler> _logger;

    public UpdateDeviceParameterCommandHandler(
        IApplicationDbContext context,
        IDeviceRpcExecutorService rpcExecutor,
        IIdentityService identityService,
        IUser currentUser,
        ILogger<UpdateDeviceParameterCommandHandler> logger)
    {
        _context = context;
        _rpcExecutor = rpcExecutor;
        _identityService = identityService;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<UpdateDeviceParameterResult> Handle(UpdateDeviceParameterCommand request, CancellationToken cancellationToken)
    {
        // Find SetupDevice by DeviceId
        var setupDevice = await _context.SetupDevices
            .Include(sd => sd.Device)
            .FirstOrDefaultAsync(sd => sd.DeviceId == request.DeviceId, cancellationToken);

        if (setupDevice?.Device == null)
        {
            return new UpdateDeviceParameterResult
            {
                Success = false,
                ErrorMessage = "Device not found"
            };
        }

        // Get the parameter value record
        var parameterValue = await _context.SetupDeviceParameterValues
            .Include(sdpv => sdpv.Parameter)
            .FirstOrDefaultAsync(sdpv =>
                sdpv.SetupDeviceId == setupDevice.Id &&
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

        Guard.Against.Null(parameterValue.Parameter);

        // RBAC: Check if user has permission to update this parameter
        var parameter = parameterValue.Parameter;
        if (parameter.RoleType == "Admin" && _currentUser.Id != null)
        {
            var isAdmin = await _identityService.IsInRoleAsync(_currentUser.Id, Roles.Administrator);
            if (!isAdmin)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to update admin-only parameter {ParameterName}",
                    _currentUser.Id,
                    parameter.Name);

                return new UpdateDeviceParameterResult
                {
                    Success = false,
                    ErrorMessage = "Access denied. This parameter requires administrator privileges.",
                    ParameterName = parameter.Name
                };
            }
        }

        var device = setupDevice.Device;
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
