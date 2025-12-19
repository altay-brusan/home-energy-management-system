using Hems_Backend.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.DeviceSettings.Commands.UpdateDeviceParameters;

public record UpdateDeviceParametersCommand : IRequest<UpdateDeviceParametersResult>
{
    public Guid SetupDeviceId { get; init; }
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

    public UpdateDeviceParametersCommandHandler(
        IApplicationDbContext context,
        IDeviceRpcExecutorService rpcExecutor)
    {
        _context = context;
        _rpcExecutor = rpcExecutor;
    }

    public async Task<UpdateDeviceParametersResult> Handle(UpdateDeviceParametersCommand request, CancellationToken cancellationToken)
    {
        // Get all parameter values for this device
        var parameterIds = request.Parameters.Select(p => p.ParameterId).ToList();

        var parameterValues = await _context.SetupDeviceParameterValues
            .Include(p => p.Parameter)
            .Include(p => p.SetupDevice)
            .Where(p =>
                p.SetupDeviceId == request.SetupDeviceId &&
                parameterIds.Contains(p.ParameterId))
            .ToListAsync(cancellationToken);

        if (!parameterValues.Any())
        {
            return new UpdateDeviceParametersResult
            {
                Success = false,
                ErrorMessage = "No parameters found for this device"
            };
        }

        // Get device ID
        var deviceId = parameterValues.First().SetupDevice.DeviceId;

        // Build parameter dictionary for RPC
        var parameterUpdates = new Dictionary<string, string>();
        var parameterValueMap = new Dictionary<string, Domain.Entities.SetupDeviceParameterValue>();

        foreach (var update in request.Parameters)
        {
            var paramValue = parameterValues.FirstOrDefault(p => p.ParameterId == update.ParameterId);
            if (paramValue != null)
            {
                parameterUpdates[paramValue.Parameter.Name] = update.Value;
                parameterValueMap[paramValue.Parameter.Name] = paramValue;
            }
        }

        if (!parameterUpdates.Any())
        {
            return new UpdateDeviceParametersResult
            {
                Success = false,
                ErrorMessage = "No valid parameters to update"
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
            return new UpdateDeviceParametersResult
            {
                Success = false,
                ErrorMessage = $"Failed to update device: {errorMessages}",
                SuccessfulUpdates = rpcResults
                    .Where(r => r.Success)
                    .SelectMany(r => r.UpdatedParameters)
                    .ToList(),
                FailedUpdates = rpcResults
                    .Where(r => !r.Success)
                    .SelectMany(r => r.UpdatedParameters)
                    .ToList()
            };
        }

        // If all RPC calls succeeded, update database
        var updatedParameters = new List<UpdatedParameterInfo>();

        foreach (var kvp in parameterUpdates)
        {
            if (parameterValueMap.TryGetValue(kvp.Key, out var paramValue))
            {
                paramValue.Value = kvp.Value;
                updatedParameters.Add(new UpdatedParameterInfo
                {
                    ParameterId = paramValue.ParameterId,
                    ParameterName = kvp.Key,
                    NewValue = kvp.Value
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new UpdateDeviceParametersResult
        {
            Success = true,
            UpdatedParameters = updatedParameters,
            RpcMethods = rpcResults.Select(r => r.RpcMethod).Distinct().ToList()
        };
    }
}

public class UpdateDeviceParametersResult
{
    public bool Success { get; set; }
    public List<UpdatedParameterInfo> UpdatedParameters { get; set; } = new();
    public List<string> RpcMethods { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public List<string> SuccessfulUpdates { get; set; } = new();
    public List<string> FailedUpdates { get; set; } = new();
}

public class UpdatedParameterInfo
{
    public Guid ParameterId { get; set; }
    public string ParameterName { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
}
