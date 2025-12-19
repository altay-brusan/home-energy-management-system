using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.DeviceSettings.Commands.ChangeExportStrategy;
using Hems_Backend.Application.DeviceSettings.Commands.SetDeviceMode;
using Hems_Backend.Application.DeviceSettings.Commands.UpdateDeviceParameter;
using Hems_Backend.Application.DeviceSettings.Commands.UpdateDeviceParameters;
using Hems_Backend.Application.DeviceSettings.Queries.GetDeviceModes;
using Hems_Backend.Application.DeviceSettings.Queries.GetDeviceParameters;
using Hems_Backend.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;

namespace Hems_Backend.Web.Endpoints;

public class DeviceSettings : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .RequireAuthorization()
            .MapPost(ChangeExportStrategy, "ChangeExportStrategy")
            .MapGet(GetDeviceParameters, "{setupDeviceId:guid}/parameters")
            .MapPut(UpdateDeviceParameter, "{setupDeviceId:guid}/parameters/{parameterId:guid}")
            .MapPut(UpdateDeviceParameters, "{setupDeviceId:guid}/parameters")
            .MapGet(GetAvailableModes, "modes")
            .MapPost(SetDeviceMode, "{setupDeviceId:guid}/mode");
    }

    /// <summary>
    /// This endpoint allows changing the export strategy of a device.
    /// It is for demonstration purposes only and may be removed in future releases.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="command"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Get all parameters for a setup device with their current values
    /// </summary>
    [Authorize]
    public async Task<IResult> GetDeviceParameters(
        ISender sender,
        Guid setupDeviceId)
    {
        var query = new GetDeviceParametersQuery { SetupDeviceId = setupDeviceId };
        var result = await sender.Send(query);
        return Results.Ok(result);
    }

    /// <summary>
    /// Update a single device parameter
    /// Updates the device via RPC first, then updates the database if successful
    /// </summary>
    [Authorize]
    public async Task<IResult> UpdateDeviceParameter(
        ISender sender,
        Guid setupDeviceId,
        Guid parameterId,
        UpdateDeviceParameterRequest request)
    {
        var command = new UpdateDeviceParameterCommand
        {
            SetupDeviceId = setupDeviceId,
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
        Guid setupDeviceId,
        UpdateDeviceParametersRequest request)
    {
        var command = new UpdateDeviceParametersCommand
        {
            SetupDeviceId = setupDeviceId,
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
        Guid setupDeviceId,
        SetDeviceModeRequest request)
    {
        var command = new SetDeviceModeCommand
        {
            SetupDeviceId = setupDeviceId,
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
            message = result.ErrorMessage ?? "Failed to set device mode",
            successfulParameters = result.SuccessfulParameters,
            failedParameters = result.FailedParameters
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
    public List<ParameterUpdate> Parameters { get; init; } = new();
}

public record SetDeviceModeRequest
{
    public Hems_Backend.Application.DeviceSettings.Models.DeviceMode Mode { get; init; }
}
