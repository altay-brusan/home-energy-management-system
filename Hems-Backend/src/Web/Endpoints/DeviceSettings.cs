using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.DeviceSettings.Commands.ChangeExportStrategy;
using Hems_Backend.Application.DeviceSettings.Commands.SetDeviceMode;
using Hems_Backend.Application.DeviceSettings.Commands.UpdateDeviceParameter;
using Hems_Backend.Application.DeviceSettings.Commands.UpdateDeviceParameters;
using Hems_Backend.Application.DeviceSettings.Commands.CreateSetupDeviceParameterValue;
using Hems_Backend.Application.DeviceSettings.Commands.DeleteSetupDeviceParameterValue;
using Hems_Backend.Application.DeviceSettings.Commands.UpdateSetupDeviceParameterValue;
using Hems_Backend.Application.DeviceSettings.Commands.UpdateDeviceParameterValue;
using Hems_Backend.Application.DeviceSettings.Commands.InitializeDeviceSettings;
using Hems_Backend.Application.DeviceSettings.Queries.GetDeviceModes;
using Hems_Backend.Application.DeviceSettings.Queries.GetDeviceParameters;
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
            // ==== RPC-SYNCED OPERATIONS (New - Recommended for users) ====
            .MapGet(GetDeviceParameters, "device/{deviceId:guid}/parameters")
            .MapGet(GetDeviceParameterValuesTree, "device/{deviceId:guid}/tree")
            .MapPut(UpdateDeviceParameter, "device/{deviceId:guid}/parameters/{parameterId:guid}")
            .MapPut(UpdateDeviceParameters, "device/{deviceId:guid}/parameters")
            .MapGet(GetAvailableModes, "modes")
            .MapPost(SetDeviceMode, "device/{deviceId:guid}/mode")

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
    // Recommended for all user-facing operations
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
    /// Update multiple device parameters in batch
    /// Groups parameters by RPC method to minimize RPC calls
    /// Updates the device via RPC first, then updates the database if successful
    /// </summary>
    [Authorize]
    public async Task<IResult> UpdateDeviceParameters(
        ISender sender,
        Guid deviceId,
        UpdateDeviceParametersRequest request)
    {
        var command = new UpdateDeviceParametersCommand
        {
            DeviceId = deviceId,
            Parameters = request.Parameters
        };

        var result = await sender.Send(command);

        if (result.Success)
        {
            return Results.Ok(result);
        }

        return Results.BadRequest(new
        {
            success = false,
            message = result.ErrorMessage ?? "Failed to update parameters",
            successfulUpdates = result.SuccessfulUpdates,
            failedUpdates = result.FailedUpdates
        });
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
