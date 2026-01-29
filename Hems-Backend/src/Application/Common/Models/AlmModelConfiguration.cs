using Hems_Backend.Domain.Constants;
using Hems_Backend.Domain.Enums;

namespace Hems_Backend.Application.Common.Models;

/// <summary>
/// Configuration for each ALM model type
/// </summary>
public class AlmModelConfig
{
    /// <summary>
    /// AI container endpoint path (e.g., "/module-tsf-pc")
    /// </summary>
    public string ModuleEndpoint { get; init; } = string.Empty;

    /// <summary>
    /// Column name in DevicePowerData table (e.g., "LoadTotalPowerW")
    /// </summary>
    public string DatabaseColumn { get; init; } = string.Empty;

    /// <summary>
    /// ALM feature name used in API (e.g., "global_active_power")
    /// </summary>
    public string FeatureName { get; init; } = string.Empty;

    /// <summary>
    /// User-friendly display name (e.g., "power-consumption")
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;
}

/// <summary>
/// Maps AlmModelType to configuration settings for each model
/// </summary>
public static class AlmModelConfiguration
{
    private static readonly Dictionary<AlmModelType, AlmModelConfig> _configs = new()
    {
        [AlmModelType.PowerConsumption] = new AlmModelConfig
        {
            ModuleEndpoint = "/module-tsf-pc",
            DatabaseColumn = AlmFeatures.LoadTotalPowerW,
            FeatureName = "global_active_power",
            DisplayName = "power-consumption"
        }
        // Future model types:
        // [AlmModelType.PvGeneration] = new AlmModelConfig
        // {
        //     ModuleEndpoint = "/module-tsf-pv",
        //     DatabaseColumn = "PvTotalPowerW",
        //     FeatureName = "pv_power",
        //     DisplayName = "pv-generation"
        // },
        // [AlmModelType.GridPower] = new AlmModelConfig
        // {
        //     ModuleEndpoint = "/module-tsf-grid",
        //     DatabaseColumn = "GridTotalPowerW",
        //     FeatureName = "grid_power",
        //     DisplayName = "grid-power"
        // }
    };

    /// <summary>
    /// Gets the configuration for a specific model type
    /// </summary>
    /// <param name="modelType">The ALM model type</param>
    /// <returns>Configuration for the model type</returns>
    /// <exception cref="ArgumentException">Thrown when model type is not configured</exception>
    public static AlmModelConfig GetConfig(AlmModelType modelType)
    {
        if (_configs.TryGetValue(modelType, out var config))
        {
            return config;
        }

        throw new ArgumentException($"No configuration found for model type: {modelType}", nameof(modelType));
    }

    /// <summary>
    /// Parses a display name string to an AlmModelType
    /// </summary>
    /// <param name="displayName">Display name (e.g., "power-consumption")</param>
    /// <returns>The matching AlmModelType, or null if not found</returns>
    public static AlmModelType? FromDisplayName(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return null;
        }

        var normalizedName = displayName.Trim().ToLowerInvariant();

        foreach (var kvp in _configs)
        {
            if (kvp.Value.DisplayName.Equals(normalizedName, StringComparison.OrdinalIgnoreCase))
            {
                return kvp.Key;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets all configured model types
    /// </summary>
    public static IEnumerable<AlmModelType> GetAllModelTypes() => _configs.Keys;

    /// <summary>
    /// Checks if a model type is configured
    /// </summary>
    public static bool IsConfigured(AlmModelType modelType) => _configs.ContainsKey(modelType);
}
