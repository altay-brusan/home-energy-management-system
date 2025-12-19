using System.Text.Json;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.DeviceSettings.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Infrastructure.Services;

public class DeviceRpcExecutorService : IDeviceRpcExecutorService
{
    private readonly IThingsBoardConnectorService _tbService;
    private readonly IApplicationDbContext _context;
    private readonly ILogger<DeviceRpcExecutorService> _logger;

    public DeviceRpcExecutorService(
        IThingsBoardConnectorService tbService,
        IApplicationDbContext context,
        ILogger<DeviceRpcExecutorService> logger)
    {
        _tbService = tbService;
        _context = context;
        _logger = logger;
    }

    public async Task<RpcExecutionResult> ExecuteParameterUpdateAsync(
        Guid deviceId,
        string parameterName,
        string value,
        CancellationToken cancellationToken = default)
    {
        // Get the mapping for this parameter
        var mapping = ParameterRpcMappings.GetMapping(parameterName);
        if (mapping == null)
        {
            _logger.LogWarning("No RPC mapping found for parameter: {ParameterName}", parameterName);
            return new RpcExecutionResult
            {
                Success = false,
                ErrorMessage = $"No RPC mapping found for parameter: {parameterName}"
            };
        }

        // Get device ThingsBoard ID
        var device = await _context.Devices
            .FirstOrDefaultAsync(d => d.Id == deviceId, cancellationToken);

        if (device == null)
        {
            return new RpcExecutionResult
            {
                Success = false,
                ErrorMessage = $"Device not found: {deviceId}"
            };
        }

        // First, get current configuration from device
        var currentConfig = await GetCurrentConfigAsync(
            device.ThingsBoardDeviceId,
            mapping.RpcGetMethod,
            cancellationToken);

        if (currentConfig == null)
        {
            return new RpcExecutionResult
            {
                Success = false,
                RpcMethod = mapping.RpcGetMethod,
                ErrorMessage = "Failed to get current device configuration"
            };
        }

        // Update the specific field in the configuration
        var updatedConfig = UpdateFieldValue(currentConfig, mapping.FieldPath, value, parameterName, mapping);

        // Send updated configuration to device
        var rpcResponse = await _tbService.SendTwoWayRpcAsync(
            deviceId: device.ThingsBoardDeviceId,
            method: mapping.RpcMethod,
            parameters: updatedConfig,
            timeoutMs: 10000,
            cancellationToken: cancellationToken
        );

        if (rpcResponse == null || !rpcResponse.Success)
        {
            return new RpcExecutionResult
            {
                Success = false,
                RpcMethod = mapping.RpcMethod,
                ErrorMessage = rpcResponse?.Error ?? "RPC call failed"
            };
        }

        return new RpcExecutionResult
        {
            Success = true,
            RpcMethod = mapping.RpcMethod,
            UpdatedParameters = new List<string> { parameterName },
            ResponsePayload = rpcResponse.Response
        };
    }

    public async Task<List<RpcExecutionResult>> ExecuteParameterUpdatesAsync(
        Guid deviceId,
        Dictionary<string, string> parameters,
        CancellationToken cancellationToken = default)
    {
        var results = new List<RpcExecutionResult>();

        // Get device ThingsBoard ID
        var device = await _context.Devices
            .FirstOrDefaultAsync(d => d.Id == deviceId, cancellationToken);

        if (device == null)
        {
            results.Add(new RpcExecutionResult
            {
                Success = false,
                ErrorMessage = $"Device not found: {deviceId}"
            });
            return results;
        }

        // Group parameters by RPC method to minimize RPC calls
        var groupedByRpc = parameters
            .Select(p => new
            {
                ParameterName = p.Key,
                Value = p.Value,
                Mapping = ParameterRpcMappings.GetMapping(p.Key)
            })
            .Where(p => p.Mapping != null)
            .GroupBy(p => p.Mapping!.RpcMethod);

        foreach (var rpcGroup in groupedByRpc)
        {
            var rpcMethod = rpcGroup.Key;
            var firstMapping = rpcGroup.First().Mapping!;

            try
            {
                // Get current configuration from device
                var currentConfig = await GetCurrentConfigAsync(
                    device.ThingsBoardDeviceId,
                    firstMapping.RpcGetMethod,
                    cancellationToken);

                if (currentConfig == null)
                {
                    results.Add(new RpcExecutionResult
                    {
                        Success = false,
                        RpcMethod = rpcMethod,
                        UpdatedParameters = rpcGroup.Select(p => p.ParameterName).ToList(),
                        ErrorMessage = "Failed to get current device configuration"
                    });
                    continue;
                }

                // Update all fields in this RPC group
                var updatedConfig = currentConfig;
                foreach (var param in rpcGroup)
                {
                    updatedConfig = UpdateFieldValue(
                        updatedConfig,
                        param.Mapping!.FieldPath,
                        param.Value,
                        param.ParameterName,
                        param.Mapping);
                }

                // Send updated configuration to device
                var rpcResponse = await _tbService.SendTwoWayRpcAsync(
                    deviceId: device.ThingsBoardDeviceId,
                    method: rpcMethod,
                    parameters: updatedConfig,
                    timeoutMs: 10000,
                    cancellationToken: cancellationToken
                );

                if (rpcResponse == null || !rpcResponse.Success)
                {
                    results.Add(new RpcExecutionResult
                    {
                        Success = false,
                        RpcMethod = rpcMethod,
                        UpdatedParameters = rpcGroup.Select(p => p.ParameterName).ToList(),
                        ErrorMessage = rpcResponse?.Error ?? "RPC call failed"
                    });
                    continue;
                }

                results.Add(new RpcExecutionResult
                {
                    Success = true,
                    RpcMethod = rpcMethod,
                    UpdatedParameters = rpcGroup.Select(p => p.ParameterName).ToList(),
                    ResponsePayload = rpcResponse.Response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing RPC method {RpcMethod}", rpcMethod);
                results.Add(new RpcExecutionResult
                {
                    Success = false,
                    RpcMethod = rpcMethod,
                    UpdatedParameters = rpcGroup.Select(p => p.ParameterName).ToList(),
                    ErrorMessage = ex.Message
                });
            }
        }

        return results;
    }

    public async Task<string?> GetParameterValueAsync(
        Guid deviceId,
        string parameterName,
        CancellationToken cancellationToken = default)
    {
        var mapping = ParameterRpcMappings.GetMapping(parameterName);
        if (mapping == null)
        {
            _logger.LogWarning("No RPC mapping found for parameter: {ParameterName}", parameterName);
            return null;
        }

        var device = await _context.Devices
            .FirstOrDefaultAsync(d => d.Id == deviceId, cancellationToken);

        if (device == null)
        {
            return null;
        }

        var currentConfig = await GetCurrentConfigAsync(
            device.ThingsBoardDeviceId,
            mapping.RpcGetMethod,
            cancellationToken);

        if (currentConfig == null)
        {
            return null;
        }

        return GetFieldValue(currentConfig, mapping.FieldPath);
    }

    public async Task<Dictionary<string, string?>> GetParameterValuesAsync(
        Guid deviceId,
        List<string> parameterNames,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, string?>();

        var device = await _context.Devices
            .FirstOrDefaultAsync(d => d.Id == deviceId, cancellationToken);

        if (device == null)
        {
            return result;
        }

        // Group parameters by RPC get method to minimize RPC calls
        var groupedByRpc = parameterNames
            .Select(p => new
            {
                ParameterName = p,
                Mapping = ParameterRpcMappings.GetMapping(p)
            })
            .Where(p => p.Mapping != null)
            .GroupBy(p => p.Mapping!.RpcGetMethod);

        foreach (var rpcGroup in groupedByRpc)
        {
            try
            {
                var currentConfig = await GetCurrentConfigAsync(
                    device.ThingsBoardDeviceId,
                    rpcGroup.Key,
                    cancellationToken);

                if (currentConfig == null)
                {
                    foreach (var param in rpcGroup)
                    {
                        result[param.ParameterName] = null;
                    }
                    continue;
                }

                foreach (var param in rpcGroup)
                {
                    var value = GetFieldValue(currentConfig, param.Mapping!.FieldPath);
                    result[param.ParameterName] = value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting parameter values for RPC method {RpcMethod}", rpcGroup.Key);
                foreach (var param in rpcGroup)
                {
                    result[param.ParameterName] = null;
                }
            }
        }

        return result;
    }

    // ============ Private Helper Methods ============

    private async Task<Dictionary<string, object>?> GetCurrentConfigAsync(
        Guid thingsBoardDeviceId,
        string getRpcMethod,
        CancellationToken cancellationToken)
    {
        try
        {
            var rpcResponse = await _tbService.SendTwoWayRpcAsync(
                deviceId: thingsBoardDeviceId,
                method: getRpcMethod,
                parameters: new Dictionary<string, object>(),
                timeoutMs: 10000,
                cancellationToken: cancellationToken
            );

            if (rpcResponse?.Success == true && rpcResponse.Response != null)
            {
                return rpcResponse.Response;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current config with RPC method {RpcMethod}", getRpcMethod);
            return null;
        }
    }

    private Dictionary<string, object> UpdateFieldValue(
        Dictionary<string, object> config,
        string fieldPath,
        string value,
        string parameterName,
        RpcMethodMapping mapping)
    {
        // Handle custom mappings
        if (mapping.RequiresCustomMapping)
        {
            return ApplyCustomMapping(config, fieldPath, value, parameterName, mapping.CustomMappingFunction);
        }

        // Handle nested field paths (e.g., "inverterMode.solarSellEnabled")
        var pathParts = fieldPath.Split('.');
        var current = config;

        for (int i = 0; i < pathParts.Length - 1; i++)
        {
            var part = pathParts[i];
            if (!current.ContainsKey(part))
            {
                current[part] = new Dictionary<string, object>();
            }

            if (current[part] is not Dictionary<string, object> dict)
            {
                dict = new Dictionary<string, object>();
                current[part] = dict;
            }
            current = dict;
        }

        // Set the final value with appropriate type conversion
        var lastPart = pathParts[^1];
        current[lastPart] = ConvertValue(value);

        return config;
    }

    private string? GetFieldValue(Dictionary<string, object> config, string fieldPath)
    {
        var pathParts = fieldPath.Split('.');
        object? current = config;

        foreach (var part in pathParts)
        {
            if (current is not Dictionary<string, object> dict || !dict.ContainsKey(part))
            {
                return null;
            }
            current = dict[part];
        }

        return current?.ToString();
    }

    private object ConvertValue(string value)
    {
        // Try to parse as boolean
        if (bool.TryParse(value, out var boolValue))
        {
            return boolValue;
        }

        // Try to parse as integer
        if (int.TryParse(value, out var intValue))
        {
            return intValue;
        }

        // Try to parse as decimal
        if (decimal.TryParse(value, out var decimalValue))
        {
            return decimalValue;
        }

        // Return as string
        return value;
    }

    private Dictionary<string, object> ApplyCustomMapping(
        Dictionary<string, object> config,
        string fieldPath,
        string value,
        string parameterName,
        string? customMappingFunction)
    {
        // Handle special cases like ExportStrategy that require mapping
        if (customMappingFunction == "MapExportStrategy")
        {
            // Map from user-friendly values to device codes
            var exportStrategyMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["SellFirst"] = "selling_first",
                ["SellingFirst"] = "selling_first",
                ["selling_first"] = "selling_first",
                ["ZeroExportToLoad"] = "zero_export_to_load",
                ["zero_export_to_load"] = "zero_export_to_load",
                ["ZeroExportToCT"] = "zero_export_to_ct",
                ["ZeroExportToCt"] = "zero_export_to_ct",
                ["zero_export_to_ct"] = "zero_export_to_ct"
            };

            if (exportStrategyMap.TryGetValue(value, out var mappedValue))
            {
                config[fieldPath] = mappedValue;
            }
            else
            {
                // If no mapping found, use the value as-is
                config[fieldPath] = value;
            }
        }
        else
        {
            // Default behavior for unknown custom mappings
            config[fieldPath] = ConvertValue(value);
        }

        return config;
    }
}
