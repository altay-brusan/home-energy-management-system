using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Constants;
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
    private readonly IIdentityService _identityService;
    private readonly IUser _currentUser;
    private readonly ILogger<UpdateDeviceParametersCommandHandler> _logger;

    public UpdateDeviceParametersCommandHandler(
        IApplicationDbContext context,
        IDeviceRpcExecutorService rpcExecutor,
        IIdentityService identityService,
        IUser currentUser,
        ILogger<UpdateDeviceParametersCommandHandler> logger)
    {
        _context = context;
        _rpcExecutor = rpcExecutor;
        _identityService = identityService;
        _currentUser = currentUser;
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

        // RBAC: Check if user is admin (needed for admin-only parameters)
        bool isAdmin = false;
        if (_currentUser.Id != null)
        {
            isAdmin = await _identityService.IsInRoleAsync(_currentUser.Id, Roles.Administrator);
        }

        // Build parameter dictionary for RPC call (parameterName -> value)
        var parameterUpdates = new Dictionary<string, string>();
        var parameterValueMap = new Dictionary<string, Domain.Entities.SetupDeviceParameterValue>();
        var unauthorizedParameters = new List<string>();

        foreach (var update in request.Parameters)
        {
            var paramValue = parameterValues.FirstOrDefault(pv => pv.ParameterId == update.ParameterId);
            if (paramValue?.Parameter != null)
            {
                // RBAC: Check if user has permission to update this parameter
                if (paramValue.Parameter.RoleType == "Admin" && !isAdmin)
                {
                    unauthorizedParameters.Add(paramValue.Parameter.Name);
                    _logger.LogWarning(
                        "User {UserId} attempted to update admin-only parameter {ParameterName}",
                        _currentUser.Id,
                        paramValue.Parameter.Name);
                    continue; // Skip this parameter
                }

                parameterUpdates[paramValue.Parameter.Name] = update.Value;
                parameterValueMap[paramValue.Parameter.Name] = paramValue;
            }
        }

        // If there are unauthorized parameters, return error
        if (unauthorizedParameters.Count > 0)
        {
            var authorizationFailures = unauthorizedParameters.Select(name => new ParameterUpdateStatus
            {
                ParameterName = name,
                Success = false,
                ErrorMessage = "Access denied. This parameter requires administrator privileges."
            }).ToList();

            return new UpdateDeviceParametersResult
            {
                Success = false,
                ErrorMessage = $"Access denied for {unauthorizedParameters.Count} admin-only parameter(s).",
                FailedUpdates = authorizationFailures
            };
        }

        // If no parameters left after authorization check
        if (parameterUpdates.Count == 0)
        {
            return new UpdateDeviceParametersResult
            {
                Success = false,
                ErrorMessage = "No parameters to update after authorization check"
            };
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
