using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.DeviceSettings.Commands.ChangeExportStrategy;
using Hems_Backend.Application.DeviceSettings.Commands.SetDeviceMode;
using Hems_Backend.Application.DeviceSettings.Commands.SetEnergyPrice;
using Hems_Backend.Application.DeviceSettings.Commands.UpdateDeviceParameter;
using Hems_Backend.Application.DeviceSettings.Commands.UpdateDeviceParameters;
using Hems_Backend.Application.DeviceSettings.Commands.UpdateGridConfig;
using Hems_Backend.Application.DeviceSettings.Commands.CreateSetupDeviceParameterValue;
using Hems_Backend.Application.DeviceSettings.Commands.DeleteSetupDeviceParameterValue;
using Hems_Backend.Application.DeviceSettings.Commands.UpdateSetupDeviceParameterValue;
using Hems_Backend.Application.DeviceSettings.Commands.UpdateDeviceParameterValue;
using Hems_Backend.Application.DeviceSettings.Commands.InitializeDeviceSettings;
using Hems_Backend.Application.DeviceSettings.Queries.GetCurrentDeviceMode;
using Hems_Backend.Application.DeviceSettings.Queries.GetDeviceModes;
using Hems_Backend.Application.DeviceSettings.Queries.GetDeviceParameters;
using Hems_Backend.Application.DeviceSettings.Queries.GetEnergyPrices;
using Hems_Backend.Application.DeviceSettings.Queries.GetGridConfig;
using Hems_Backend.Application.DeviceSettings.Queries.GetSetupDeviceParameterValues;
using Hems_Backend.Application.DeviceSettings.Queries.GetSetupDeviceParameterValuesTree;
using Hems_Backend.Application.DeviceSettings.Queries.GetDeviceParameterValuesTree;
using Hems_Backend.Domain.Constants;
using Hems_Backend.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Web.Endpoints;

/// <summary>
/// Device Settings API - Manages actual device parameter values and configurations
/// This endpoint handles all operations related to device instance settings (not templates/schemas)
/// </summary>
public class DeviceSettings : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .RequireAuthorization()
            // ==== RPC-SYNCED OPERATIONS (Synchronous - waits for completion) ====
            .MapGet(GetDeviceParameters, "device/{deviceId:guid}/parameters")
            .MapGet(GetDeviceParameterValuesTree, "device/{deviceId:guid}/tree")
            .MapPut(UpdateDeviceParameter, "device/{deviceId:guid}/parameters/{parameterId:guid}")
            .MapPut(UpdateDeviceParameters, "device/{deviceId:guid}/parameters")
            .MapGet(GetAvailableModes, "modes")
            .MapGet(GetCurrentDeviceMode, "device/{deviceId:guid}/mode")
            .MapPost(SetDeviceMode, "device/{deviceId:guid}/mode")

            // ==== GRID CONFIG OPERATIONS (Fire-and-forget - causes device restart) ====
            .MapGet(GetGridConfig, "device/{deviceId:guid}/grid-config")
            .MapPut(UpdateGridConfig, "device/{deviceId:guid}/grid-config")

            // ==== PRICE SETTINGS ====
            .MapGet(GetEnergyPrices, "device/{deviceId:guid}/price")
            .MapPut(SetEnergyPrices, "device/{deviceId:guid}/price")

            // ==== DATABASE-ONLY OPERATIONS (Admin tools) ====
            .MapGet(GetSetupDeviceParameterValues, "admin")
            .MapGet(GetSetupDeviceParameterValuesTree, "admin/tree")
            .MapPost(CreateSetupDeviceParameterValue, "admin")
            .MapPut(UpdateSetupDeviceParameterValue, "admin/{id}")
            .MapDelete(DeleteSetupDeviceParameterValue, "admin/{id}")
            .MapPost(InitializeDeviceSettings, "admin/device/{deviceId}/initialize")
            .MapPut(UpdateDeviceParameterValueRaw, "admin/device/{deviceId}/parameter/{parameterId}")

            // ==== DEMO/TESTING ====
            .MapPost(ChangeExportStrategy, "demo/export-strategy");
    }

    // ========================================================================
    // RPC-SYNCED OPERATIONS - Updates both device and database
    // Now synchronous - waits for completion and returns complete result
    // ========================================================================

    /// <summary>
    /// Get all parameters for a device with their current values
    /// </summary>
    /// <param name="sender">MediatR sender</param>
    /// <param name="deviceId">Device ID</param>
    /// <returns>List of device parameters with current values</returns>
    [Authorize]
    public async Task<IResult> GetDeviceParameters(
        ISender sender,
        Guid deviceId)
    {
        var query = new GetDeviceParametersQuery { DeviceId = deviceId };
        var result = await sender.Send(query);
        return Results.Ok(result);
    }

    /// <summary>
    /// Update a single device parameter
    /// Updates the device via RPC first, then updates the database if successful
    /// </summary>
    /// <param name="sender">MediatR sender</param>
    /// <param name="deviceId">Device ID</param>
    /// <param name="parameterId">Parameter ID</param>
    /// <param name="request">Update request with new value</param>
    [Authorize]
    public async Task<IResult> UpdateDeviceParameter(
        ISender sender,
        Guid deviceId,
        Guid parameterId,
        UpdateDeviceParameterRequest request)
    {
        var command = new UpdateDeviceParameterCommand
        {
            DeviceId = deviceId,
            ParameterId = parameterId,
            Value = request.Value
        };

        var result = await sender.Send(command);

        if (result.Success)
        {
            return Results.Ok(result);
        }

        return Results.BadRequest(new
        {
            success = false,
            message = result.ErrorMessage ?? "Failed to update parameter"
        });
    }

    /// <summary>
    /// Update multiple device parameters in batch (synchronous)
    /// Waits for all updates to complete and returns the complete result.
    /// Timeout: 3 minutes (handles up to ~50 parameters safely).
    /// </summary>
    /// <remarks>
    /// This endpoint is now synchronous - it waits for all parameter updates to complete
    /// before returning. The response includes:
    /// - success: true if all parameters updated successfully
    /// - totalParameters: number of parameters in the request
    /// - successCount: number of parameters successfully updated
    /// - failedCount: number of parameters that failed
    /// - failedParameters: details of each failed parameter
    /// - duration: total time taken for the operation
    ///
    /// If the device is busy (another update in progress), returns 400 with errorCode "DEVICE_BUSY"
    /// </remarks>
    [Authorize]
    public async Task<IResult> UpdateDeviceParameters(
        ISender sender,
        ILogger<DeviceSettings> logger,
        Guid deviceId,
        UpdateDeviceParametersRequest request,
        CancellationToken cancellationToken)
    {
        // DEBUG: Log incoming request
        logger.LogInformation(
            "📥 [Endpoint] UpdateDeviceParameters called: DeviceId={DeviceId}, ParameterCount={Count}, Parameters=[{Params}]",
            deviceId,
            request.Parameters.Count,
            string.Join("; ", request.Parameters.Select(p => $"Id={p.ParameterId}, Value='{p.Value}'")));

        // Create a timeout token (3 minutes) to prevent indefinite hangs
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromMinutes(3));

        var command = new UpdateDeviceParametersCommand
        {
            DeviceId = deviceId,
            Parameters = request.Parameters
        };

        try
        {
            var result = await sender.Send(command, timeoutCts.Token);

            // DEBUG: Log result
            logger.LogInformation(
                "📤 [Endpoint] UpdateDeviceParameters result: Success={Success}, ErrorCode={ErrorCode}, ErrorMessage={ErrorMessage}, " +
                "TotalParams={Total}, SuccessCount={SuccessCount}, FailedCount={FailedCount}, FailedParams=[{FailedParams}]",
                result.Success,
                result.ErrorCode ?? "none",
                result.ErrorMessage ?? "none",
                result.TotalParameters,
                result.SuccessCount,
                result.FailedCount,
                string.Join("; ", result.FailedParameters.Select(p => $"{p.ParameterName}: {p.ErrorMessage}")));

            if (result.Success)
            {
                return Results.Ok(new
                {
                    success = true,
                    totalParameters = result.TotalParameters,
                    successCount = result.SuccessCount,
                    failedCount = result.FailedCount,
                    failedParameters = result.FailedParameters,
                    duration = result.Duration.ToString(),
                    // Restart information for Grid Standard settings
                    deviceWillRestart = result.DeviceWillRestart,
                    restartReason = result.RestartReason,
                    restartCausingParameters = result.RestartCausingParameters
                });
            }

            // Handle specific error codes
            if (result.ErrorCode is "DEVICE_BUSY" or "DEVICE_RESTARTING")
            {
                return Results.BadRequest(new
                {
                    success = false,
                    errorCode = result.ErrorCode,
                    message = result.ErrorMessage
                });
            }

            // Partial success (some parameters updated, some failed)
            if (result.SuccessCount > 0)
            {
                return Results.Ok(new
                {
                    success = false, // Not fully successful
                    totalParameters = result.TotalParameters,
                    successCount = result.SuccessCount,
                    failedCount = result.FailedCount,
                    failedParameters = result.FailedParameters,
                    duration = result.Duration.ToString(),
                    message = $"{result.SuccessCount} parameters updated, {result.FailedCount} failed",
                    // Restart information for Grid Standard settings
                    deviceWillRestart = result.DeviceWillRestart,
                    restartReason = result.RestartReason,
                    restartCausingParameters = result.RestartCausingParameters
                });
            }

            return Results.BadRequest(new
            {
                success = false,
                errorCode = result.ErrorCode,
                message = result.ErrorMessage ?? "Failed to update parameters",
                failedParameters = result.FailedParameters
            });
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            // Timeout occurred (not client cancellation)
            logger.LogWarning(
                "⏱️ [Endpoint] UpdateDeviceParameters timed out after 3 minutes: DeviceId={DeviceId}, ParameterCount={Count}",
                deviceId, request.Parameters.Count);

            return Results.StatusCode(504); // Gateway Timeout
        }
    }

    /// <summary>
    /// Get all available device modes with their descriptions
    /// </summary>
    [Authorize]
    public async Task<IResult> GetAvailableModes(ISender sender)
    {
        var query = new GetDeviceModesQuery();
        var result = await sender.Send(query);
        return Results.Ok(result);
    }

    /// <summary>
    /// Get the current operating mode for a device
    /// Returns the latest active mode from the device mode history
    /// </summary>
    [Authorize]
    public async Task<IResult> GetCurrentDeviceMode(
        ISender sender,
        Guid deviceId)
    {
        var query = new GetCurrentDeviceModeQuery { DeviceId = deviceId };
        var result = await sender.Send(query);

        if (result.Success)
        {
            return Results.Ok(result);
        }

        return Results.BadRequest(new
        {
            success = false,
            message = result.ErrorMessage ?? "Failed to get device mode"
        });
    }

    /// <summary>
    /// Set device to a predefined operating mode
    /// Applies all parameter settings for the selected mode
    /// </summary>
    [Authorize]
    public async Task<IResult> SetDeviceMode(
        ISender sender,
        Guid deviceId,
        SetDeviceModeRequest request)
    {
        var command = new SetDeviceModeCommand
        {
            DeviceId = deviceId,
            Mode = request.Mode
        };

        var result = await sender.Send(command);

        if (result.Success)
        {
            return Results.Ok(result);
        }

        return Results.BadRequest(new
        {
            success = false,
            message = result.ErrorMessage ?? "Failed to set device mode"
        });
    }

    // ========================================================================
    // GRID CONFIG OPERATIONS - Fire-and-forget for restart-causing parameters
    // ========================================================================

    /// <summary>
    /// Get grid configuration parameters for a device.
    /// Returns only grid-related parameters that cause device restart when changed.
    /// </summary>
    /// <remarks>
    /// Grid configuration includes:
    /// - Grid Standard (General, UL1741, CPUC RULE21, etc.)
    /// - Grid Frequency and Voltage settings
    /// - Phase Type and Grid Level
    /// - Reconnection Time
    /// - Power Factor settings
    ///
    /// These parameters require special handling because changing them causes
    /// the device to restart.
    /// </remarks>
    [Authorize(Policies.AdminOnly)]
    public async Task<IResult> GetGridConfig(
        ISender sender,
        Guid deviceId)
    {
        var query = new GetGridConfigQuery { DeviceId = deviceId };
        var result = await sender.Send(query);

        if (result.Success)
        {
            return Results.Ok(new
            {
                success = true,
                deviceId = result.DeviceId,
                parameters = result.Parameters
            });
        }

        // Handle specific error codes
        if (result.ErrorCode == "DEVICE_RESTARTING")
        {
            return Results.BadRequest(new
            {
                success = false,
                errorCode = result.ErrorCode,
                message = result.ErrorMessage,
                estimatedAvailableAt = result.EstimatedAvailableAt
            });
        }

        return Results.BadRequest(new
        {
            success = false,
            errorCode = result.ErrorCode,
            message = result.ErrorMessage ?? "Failed to get grid config"
        });
    }

    /// <summary>
    /// Update grid configuration parameters (fire-and-forget, causes device restart).
    /// </summary>
    /// <remarks>
    /// IMPORTANT: This endpoint uses fire-and-forget pattern because grid config
    /// changes cause the device to restart. The response includes:
    /// - deviceWillRestart: true (always, if successful)
    /// - restartGracePeriodSeconds: ~180 seconds
    /// - estimatedAvailableAt: timestamp when device should be back online
    ///
    /// Frontend should:
    /// 1. Show confirmation dialog warning about restart
    /// 2. Disable settings UI for the grace period (2-3 minutes)
    /// 3. After grace period, fetch latest values via GET /grid-config
    ///
    /// Error codes:
    /// - DEVICE_BUSY: Another operation is in progress
    /// - DEVICE_RESTARTING: Device is currently restarting from previous update
    /// - INVALID_PARAMETERS: Non-grid parameters were included (use /parameters instead)
    /// </remarks>
    [Authorize(Policies.AdminOnly)]
    public async Task<IResult> UpdateGridConfig(
        ISender sender,
        ILogger<DeviceSettings> logger,
        Guid deviceId,
        UpdateGridConfigRequest request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "📥 [Endpoint] UpdateGridConfig called: DeviceId={DeviceId}, ParameterCount={Count}",
            deviceId, request.Parameters.Count);

        var command = new UpdateGridConfigCommand
        {
            DeviceId = deviceId,
            Parameters = request.Parameters.Select(p => new GridConfigParameterUpdate
            {
                ParameterId = p.ParameterId,
                Value = p.Value
            }).ToList()
        };

        var result = await sender.Send(command, cancellationToken);

        logger.LogInformation(
            "📤 [Endpoint] UpdateGridConfig result: Success={Success}, ErrorCode={ErrorCode}, " +
            "SuccessCount={SuccessCount}, FailedCount={FailedCount}, DeviceWillRestart={DeviceWillRestart}",
            result.Success, result.ErrorCode ?? "none",
            result.SuccessCount, result.FailedCount, result.DeviceWillRestart);

        if (result.Success)
        {
            return Results.Ok(new
            {
                success = true,
                totalParameters = result.TotalParameters,
                successCount = result.SuccessCount,
                failedCount = result.FailedCount,
                appliedParameters = result.AppliedParameters,
                duration = result.Duration.ToString(),
                deviceWillRestart = result.DeviceWillRestart,
                restartReason = result.RestartReason,
                restartGracePeriodSeconds = result.RestartGracePeriodSeconds,
                estimatedAvailableAt = result.EstimatedAvailableAt
            });
        }

        // Handle specific error codes
        if (result.ErrorCode is "DEVICE_BUSY" or "DEVICE_RESTARTING")
        {
            return Results.BadRequest(new
            {
                success = false,
                errorCode = result.ErrorCode,
                message = result.ErrorMessage,
                estimatedAvailableAt = result.EstimatedAvailableAt,
                restartGracePeriodSeconds = result.RestartGracePeriodSeconds
            });
        }

        // Partial success (some parameters applied, some failed)
        if (result.SuccessCount > 0)
        {
            return Results.Ok(new
            {
                success = false,
                totalParameters = result.TotalParameters,
                successCount = result.SuccessCount,
                failedCount = result.FailedCount,
                appliedParameters = result.AppliedParameters,
                failedParameters = result.FailedParameters,
                duration = result.Duration.ToString(),
                message = $"{result.SuccessCount} parameters applied, {result.FailedCount} failed",
                deviceWillRestart = result.DeviceWillRestart,
                restartReason = result.RestartReason,
                restartGracePeriodSeconds = result.RestartGracePeriodSeconds,
                estimatedAvailableAt = result.EstimatedAvailableAt
            });
        }

        return Results.BadRequest(new
        {
            success = false,
            errorCode = result.ErrorCode,
            message = result.ErrorMessage ?? "Failed to update grid config",
            failedParameters = result.FailedParameters
        });
    }

    // ========================================================================
    // PRICE SETTINGS - Energy price configuration
    // ========================================================================

    /// <summary>
    /// Get energy prices for a device
    /// </summary>
    /// <remarks>
    /// Returns active energy price entries for the device.
    /// Each entry can have a time-of-day range (BeginHour, EndHour).
    /// </remarks>
    [Authorize]
    public async Task<IResult> GetEnergyPrices(
        ISender sender,
        Guid deviceId)
    {
        var query = new GetEnergyPricesQuery { DeviceId = deviceId };
        var result = await sender.Send(query);

        if (result.Success)
        {
            return Results.Ok(result);
        }

        return Results.BadRequest(new
        {
            success = false,
            message = result.ErrorMessage ?? "Failed to get energy prices"
        });
    }

    /// <summary>
    /// Set energy prices for a device
    /// </summary>
    /// <remarks>
    /// Sets sell and buy prices for a device. Supports time-of-day pricing.
    /// Previous prices are deactivated when new prices are set.
    ///
    /// Example request:
    /// {
    ///   "unit": "kWh",
    ///   "currency": "EUR",
    ///   "prices": [
    ///     { "sellPrice": 0.08, "buyPrice": 0.15, "beginHour": 0, "endHour": 6 },
    ///     { "sellPrice": 0.12, "buyPrice": 0.25, "beginHour": 6, "endHour": 22 },
    ///     { "sellPrice": 0.08, "buyPrice": 0.15, "beginHour": 22, "endHour": 24 }
    ///   ]
    /// }
    /// </remarks>
    [Authorize]
    public async Task<IResult> SetEnergyPrices(
        ISender sender,
        Guid deviceId,
        SetEnergyPriceRequest request)
    {
        var command = new SetEnergyPriceCommand
        {
            DeviceId = deviceId,
            Unit = request.Unit,
            Currency = request.Currency,
            Prices = request.Prices.Select(p => new PriceEntry
            {
                SellPrice = p.SellPrice,
                BuyPrice = p.BuyPrice,
                BeginHour = p.BeginHour,
                EndHour = p.EndHour
            }).ToList()
        };

        var result = await sender.Send(command);

        if (result.Success)
        {
            return Results.Ok(result);
        }

        return Results.BadRequest(new
        {
            success = false,
            message = result.ErrorMessage ?? "Failed to set energy prices"
        });
    }

    // ========================================================================
    // DATABASE-ONLY OPERATIONS (Admin) - No RPC synchronization
    // Use only for admin tools, bulk operations, or when device is offline
    // ========================================================================

    /// <summary>
    /// Get parameter values from database (no device sync)
    /// </summary>
    [Authorize(Policies.AdminOnly)]
    public async Task<SetupDeviceParameterValuesVm> GetSetupDeviceParameterValues(
        ISender sender,
        Guid setupDeviceId)
    {
        return await sender.Send(new GetSetupDeviceParameterValuesQuery { SetupDeviceId = setupDeviceId });
    }

    /// <summary>
    /// Get parameter values in tree structure (no device sync)
    /// </summary>
    [Authorize(Policies.AdminOnly)]
    public async Task<SetupDeviceParameterValuesTreeVm> GetSetupDeviceParameterValuesTree(
        ISender sender,
        Guid setupDeviceId)
    {
        return await sender.Send(new GetSetupDeviceParameterValuesTreeQuery { SetupDeviceId = setupDeviceId });
    }

    /// <summary>
    /// Get parameter values tree by device ID (no device sync)
    /// </summary>
    [Authorize]
    public async Task<SetupDeviceParameterValuesTreeVm> GetDeviceParameterValuesTree(
        ISender sender,
        Guid deviceId)
    {
        return await sender.Send(new GetDeviceParameterValuesTreeQuery { DeviceId = deviceId });
    }

    /// <summary>
    /// Create parameter value in database (no device sync)
    /// </summary>
    [Authorize(Policies.AdminOnly)]
    public async Task<Created<Guid>> CreateSetupDeviceParameterValue(
        ISender sender,
        CreateSetupDeviceParameterValueCommand command)
    {
        var id = await sender.Send(command);
        return TypedResults.Created($"/api/DeviceSettings/admin/{id}", id);
    }

    /// <summary>
    /// Update parameter value in database (no device sync)
    /// </summary>
    [Authorize(Policies.AdminOnly)]
    public async Task<Results<NoContent, BadRequest>> UpdateSetupDeviceParameterValue(
        ISender sender,
        Guid id,
        UpdateSetupDeviceParameterValueCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();

        await sender.Send(command);
        return TypedResults.NoContent();
    }

    /// <summary>
    /// Delete parameter value from database (no device sync)
    /// </summary>
    [Authorize(Policies.AdminOnly)]
    public async Task<Results<NoContent, BadRequest>> DeleteSetupDeviceParameterValue(
        ISender sender,
        Guid id)
    {
        await sender.Send(new DeleteSetupDeviceParameterValueCommand { Id = id });
        return TypedResults.NoContent();
    }

    /// <summary>
    /// Update device parameter value in database (no device sync)
    /// </summary>
    [Authorize]
    public async Task<Results<NoContent, BadRequest>> UpdateDeviceParameterValueRaw(
        ISender sender,
        Guid deviceId,
        Guid parameterId,
        UpdateDeviceParameterValueCommand command)
    {
        if (deviceId != command.DeviceId || parameterId != command.ParameterId)
            return TypedResults.BadRequest();

        await sender.Send(command);
        return TypedResults.NoContent();
    }

    /// <summary>
    /// Initializes device settings for existing devices that were created before auto-initialization was implemented.
    /// </summary>
    /// <remarks>
    /// This endpoint is for legacy/existing devices only. New devices automatically initialize their settings
    /// when they are linked to a setup via CreateSetupDevice.
    ///
    /// Use this endpoint to:
    /// - Add settings to devices that were created before the auto-initialization feature
    /// - Retry initialization if it failed for some reason
    /// - Manually populate settings for specific devices
    ///
    /// Safety: This endpoint checks if settings already exist and will not overwrite existing data.
    /// </remarks>
    [Authorize(Policies.AdminOnly)]
    public async Task<Ok<int>> InitializeDeviceSettings(
        ISender sender,
        IApplicationDbContext context,
        Guid deviceId)
    {
        // Find the SetupDevice for this Device
        var setupDevice = await context.SetupDevices
            .Include(sd => sd.Device)
            .Where(sd => sd.DeviceId == deviceId)
            .FirstOrDefaultAsync();

        if (setupDevice == null || setupDevice.Device == null)
        {
            return TypedResults.Ok(0); // No setup device found
        }

        // Initialize settings
        var count = await sender.Send(new InitializeDeviceSettingsCommand
        {
            SetupDeviceId = setupDevice.Id,
            ModelNumber = setupDevice.Device.ModelNumber
        });

        return TypedResults.Ok(count);
    }

    // ========================================================================
    // DEMO/TESTING OPERATIONS
    // ========================================================================

    /// <summary>
    /// Change export strategy of a device.
    /// This endpoint is for demonstration and testing purposes only.
    /// </summary>
    [Authorize]
    public async Task<IResult> ChangeExportStrategy(
        ISender sender,
        ChangeExportStrategyCommand command)
    {
        var response = await sender.Send(command);

        if (response != null && response.Success)
        {
            return Results.Ok(response);
        }

        if (response != null)
        {
            // Device responded but with failure
            return Results.BadRequest(response);
        }

        // No response from device (timeout, offline, etc.)
        return Results.BadRequest(new
        {
            success = false,
            message = "Failed to communicate with device. Device may be offline or timed out."
        });
    }
}

// Request DTOs
public record UpdateDeviceParameterRequest
{
    public string Value { get; init; } = string.Empty;
}

public record UpdateDeviceParametersRequest
{
    public List<Application.DeviceSettings.Commands.UpdateDeviceParameters.ParameterUpdate> Parameters { get; init; } = new();
}

public record SetDeviceModeRequest
{
    public Hems_Backend.Application.DeviceSettings.Models.DeviceMode Mode { get; init; }
}

public record SetEnergyPriceRequest
{
    /// <summary>
    /// Unit of energy measurement (e.g., "kWh")
    /// </summary>
    public string Unit { get; init; } = "kWh";

    /// <summary>
    /// Currency code (e.g., "EUR", "USD", "TRY")
    /// </summary>
    public string Currency { get; init; } = "EUR";

    /// <summary>
    /// List of price entries
    /// </summary>
    public List<PriceEntryRequest> Prices { get; init; } = new();
}

public record PriceEntryRequest
{
    /// <summary>
    /// Price at which energy is sold to the grid (export)
    /// </summary>
    public decimal SellPrice { get; init; }

    /// <summary>
    /// Price at which energy is bought from the grid (import)
    /// </summary>
    public decimal BuyPrice { get; init; }

    /// <summary>
    /// Start hour of the day when this price is valid (0-23)
    /// </summary>
    public int? BeginHour { get; init; }

    /// <summary>
    /// End hour of the day when this price is valid (0-24)
    /// </summary>
    public int? EndHour { get; init; }
}

/// <summary>
/// Request to update grid configuration parameters.
/// These parameters cause device restart when changed.
/// </summary>
public record UpdateGridConfigRequest
{
    /// <summary>
    /// List of grid config parameters to update
    /// </summary>
    public List<GridConfigParameterUpdateRequest> Parameters { get; init; } = new();
}

/// <summary>
/// Single grid config parameter update
/// </summary>
public record GridConfigParameterUpdateRequest
{
    /// <summary>
    /// Parameter ID
    /// </summary>
    public Guid ParameterId { get; init; }

    /// <summary>
    /// New value for the parameter
    /// </summary>
    public string Value { get; init; } = string.Empty;
}
