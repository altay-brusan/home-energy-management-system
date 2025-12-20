using System.Text.Json;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;

namespace Hems_Backend.Infrastructure.Services;

/// <summary>
/// Service for executing RPC commands on devices with parameter mapping
/// Handles parameter-to-RPC method mapping, batch optimization, and nested field updates
/// </summary>
public class DeviceRpcExecutorService : IDeviceRpcExecutorService
{
    private readonly IThingsBoardConnectorService _tbService;

    public DeviceRpcExecutorService(IThingsBoardConnectorService tbService)
    {
        _tbService = tbService;
    }

    public async Task<RpcExecutionResult> ExecuteParameterUpdateAsync(
        Guid tbDeviceId,
        string parameterName,
        string value,
        CancellationToken cancellationToken = default)
    {
        // Get RPC method mapping
        if (!RpcMethodMapping.TryGetMapping(parameterName, out var config) || config == null)
        {
            return new RpcExecutionResult
            {
                Success = false,
                ErrorMessage = $"No RPC mapping found for parameter: {parameterName}"
            };
        }

        try
        {
            // Get current configuration first (to preserve other fields)
            var getCurrentParams = new Dictionary<string, object>
            {
                ["kind"] = config.RpcKind
            };

            var currentResponse = await _tbService.SendTwoWayRpcAsync(
                deviceId: tbDeviceId,
                method: config.RpcGetMethod,
                parameters: getCurrentParams,
                timeoutMs: 10000,
                cancellationToken: cancellationToken
            );

            if (currentResponse == null || !currentResponse.Success || currentResponse.Response == null)
            {
                return new RpcExecutionResult
                {
                    Success = false,
                    ErrorMessage = "Failed to get current device configuration",
                    RpcMethod = config.RpcGetMethod
                };
            }

            // Parse current config
            var currentConfig = JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(currentResponse.Response)
            ) ?? new Dictionary<string, object>();

            // Update the specific field using nested path
            var mappedValue = config.ValueMapper != null ? config.ValueMapper(value) : value;
            SetNestedValue(currentConfig, config.FieldPath, mappedValue);

            // Add kind to the payload
            currentConfig["kind"] = config.RpcKind;

            // Send updated configuration
            var updateResponse = await _tbService.SendTwoWayRpcAsync(
                deviceId: tbDeviceId,
                method: config.RpcMethod,
                parameters: currentConfig,
                timeoutMs: 10000,
                cancellationToken: cancellationToken
            );

            if (updateResponse == null || !updateResponse.Success)
            {
                return new RpcExecutionResult
                {
                    Success = false,
                    ErrorMessage = "Device did not confirm parameter update",
                    RpcMethod = config.RpcMethod
                };
            }

            return new RpcExecutionResult
            {
                Success = true,
                RpcMethod = config.RpcMethod,
                Response = updateResponse.Response
            };
        }
        catch (Exception ex)
        {
            return new RpcExecutionResult
            {
                Success = false,
                ErrorMessage = $"RPC execution failed: {ex.Message}",
                RpcMethod = config.RpcMethod
            };
        }
    }

    public async Task<Dictionary<string, RpcExecutionResult>> ExecuteParameterUpdatesAsync(
        Guid tbDeviceId,
        Dictionary<string, string> parameters,
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, RpcExecutionResult>();

        // Group parameters by RPC method to minimize calls
        var parametersByMethod = parameters
            .Select(p => new
            {
                ParameterName = p.Key,
                Value = p.Value,
                Mapping = RpcMethodMapping.TryGetMapping(p.Key, out var config) ? config : null
            })
            .Where(p => p.Mapping != null)
            .GroupBy(p => new { p.Mapping!.RpcMethod, p.Mapping.RpcGetMethod, p.Mapping.RpcKind })
            .ToList();

        foreach (var group in parametersByMethod)
        {
            try
            {
                // Get current configuration
                var getCurrentParams = new Dictionary<string, object>
                {
                    ["kind"] = group.Key.RpcKind
                };

                var currentResponse = await _tbService.SendTwoWayRpcAsync(
                    deviceId: tbDeviceId,
                    method: group.Key.RpcGetMethod,
                    parameters: getCurrentParams,
                    timeoutMs: 10000,
                    cancellationToken: cancellationToken
                );

                if (currentResponse == null || !currentResponse.Success || currentResponse.Response == null)
                {
                    // Mark all parameters in this group as failed
                    foreach (var param in group)
                    {
                        results[param.ParameterName] = new RpcExecutionResult
                        {
                            Success = false,
                            ErrorMessage = "Failed to get current device configuration",
                            RpcMethod = group.Key.RpcGetMethod
                        };
                    }
                    continue;
                }

                // Parse current config
                var currentConfig = JsonSerializer.Deserialize<Dictionary<string, object>>(
                    JsonSerializer.Serialize(currentResponse.Response)
                ) ?? new Dictionary<string, object>();

                // Update all fields in this group
                foreach (var param in group)
                {
                    var mappedValue = param.Mapping!.ValueMapper != null
                        ? param.Mapping.ValueMapper(param.Value)
                        : param.Value;
                    SetNestedValue(currentConfig, param.Mapping.FieldPath, mappedValue);
                }

                // Add kind
                currentConfig["kind"] = group.Key.RpcKind;

                // Send updated configuration
                var updateResponse = await _tbService.SendTwoWayRpcAsync(
                    deviceId: tbDeviceId,
                    method: group.Key.RpcMethod,
                    parameters: currentConfig,
                    timeoutMs: 10000,
                    cancellationToken: cancellationToken
                );

                // Mark results for all parameters in this group
                foreach (var param in group)
                {
                    results[param.ParameterName] = new RpcExecutionResult
                    {
                        Success = updateResponse?.Success ?? false,
                        ErrorMessage = updateResponse?.Success == false ? "Device did not confirm update" : null,
                        RpcMethod = group.Key.RpcMethod,
                        Response = updateResponse?.Response
                    };
                }
            }
            catch (Exception ex)
            {
                // Mark all parameters in this group as failed
                foreach (var param in group)
                {
                    results[param.ParameterName] = new RpcExecutionResult
                    {
                        Success = false,
                        ErrorMessage = $"RPC execution failed: {ex.Message}",
                        RpcMethod = group.Key.RpcMethod
                    };
                }
            }
        }

        return results;
    }

    public async Task<string?> GetParameterValueAsync(
        Guid tbDeviceId,
        string parameterName,
        CancellationToken cancellationToken = default)
    {
        if (!RpcMethodMapping.TryGetMapping(parameterName, out var config) || config == null)
        {
            return null;
        }

        try
        {
            var getCurrentParams = new Dictionary<string, object>
            {
                ["kind"] = config.RpcKind
            };

            var response = await _tbService.SendTwoWayRpcAsync(
                deviceId: tbDeviceId,
                method: config.RpcGetMethod,
                parameters: getCurrentParams,
                timeoutMs: 10000,
                cancellationToken: cancellationToken
            );

            if (response == null || !response.Success || response.Response == null)
            {
                return null;
            }

            var configJson = JsonSerializer.Serialize(response.Response);
            var configDict = JsonSerializer.Deserialize<Dictionary<string, object>>(configJson);

            if (configDict == null)
            {
                return null;
            }

            var value = GetNestedValue(configDict, config.FieldPath);
            return value?.ToString();
        }
        catch
        {
            return null;
        }
    }

    public async Task<Dictionary<string, string?>> GetParameterValuesAsync(
        Guid tbDeviceId,
        List<string> parameterNames,
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, string?>();

        // Group by RPC get method to minimize calls
        var parametersByMethod = parameterNames
            .Select(name => new
            {
                ParameterName = name,
                Mapping = RpcMethodMapping.TryGetMapping(name, out var config) ? config : null
            })
            .Where(p => p.Mapping != null)
            .GroupBy(p => new { p.Mapping!.RpcGetMethod, p.Mapping.RpcKind })
            .ToList();

        foreach (var group in parametersByMethod)
        {
            try
            {
                var getCurrentParams = new Dictionary<string, object>
                {
                    ["kind"] = group.Key.RpcKind
                };

                var response = await _tbService.SendTwoWayRpcAsync(
                    deviceId: tbDeviceId,
                    method: group.Key.RpcGetMethod,
                    parameters: getCurrentParams,
                    timeoutMs: 10000,
                    cancellationToken: cancellationToken
                );

                if (response == null || !response.Success || response.Response == null)
                {
                    foreach (var param in group)
                    {
                        results[param.ParameterName] = null;
                    }
                    continue;
                }

                var configJson = JsonSerializer.Serialize(response.Response);
                var configDict = JsonSerializer.Deserialize<Dictionary<string, object>>(configJson);

                foreach (var param in group)
                {
                    var value = configDict != null ? GetNestedValue(configDict, param.Mapping!.FieldPath) : null;
                    results[param.ParameterName] = value?.ToString();
                }
            }
            catch
            {
                foreach (var param in group)
                {
                    results[param.ParameterName] = null;
                }
            }
        }

        return results;
    }

    /// <summary>
    /// Sets a value at a nested path (e.g., "inverterMode.solarSellEnabled")
    /// </summary>
    private static void SetNestedValue(Dictionary<string, object> dict, string path, object value)
    {
        var parts = path.Split('.');
        var current = dict;

        for (int i = 0; i < parts.Length - 1; i++)
        {
            if (!current.ContainsKey(parts[i]))
            {
                current[parts[i]] = new Dictionary<string, object>();
            }

            if (current[parts[i]] is JsonElement jsonElement)
            {
                var nestedDict = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonElement.GetRawText())
                    ?? new Dictionary<string, object>();
                current[parts[i]] = nestedDict;
                current = nestedDict;
            }
            else if (current[parts[i]] is Dictionary<string, object> nestedDict)
            {
                current = nestedDict;
            }
            else
            {
                var newDict = new Dictionary<string, object>();
                current[parts[i]] = newDict;
                current = newDict;
            }
        }

        current[parts[^1]] = value;
    }

    /// <summary>
    /// Gets a value from a nested path
    /// </summary>
    private static object? GetNestedValue(Dictionary<string, object> dict, string path)
    {
        var parts = path.Split('.');
        object? current = dict;

        foreach (var part in parts)
        {
            if (current is Dictionary<string, object> currentDict)
            {
                if (!currentDict.TryGetValue(part, out current))
                {
                    return null;
                }
            }
            else if (current is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.Object &&
                    jsonElement.TryGetProperty(part, out var prop))
                {
                    current = prop;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        return current;
    }
}
