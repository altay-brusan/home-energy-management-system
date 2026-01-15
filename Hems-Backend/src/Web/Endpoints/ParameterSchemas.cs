using Hems_Backend.Application.ParameterGroups.Commands.CreateParameterGroup;
using Hems_Backend.Application.ParameterGroups.Commands.DeleteParameterGroup;
using Hems_Backend.Application.ParameterGroups.Commands.UpdateParameterGroup;
using Hems_Backend.Application.ParameterGroups.Queries.GetParameterGroup;
using Hems_Backend.Application.ParameterGroups.Queries.GetParameterGroups;
using Hems_Backend.Application.Parameters.Commands.CreateParameter;
using Hems_Backend.Application.Parameters.Commands.DeleteParameter;
using Hems_Backend.Application.Parameters.Commands.UpdateParameter;
using Hems_Backend.Application.Parameters.Queries.GetParameter;
using Hems_Backend.Application.Parameters.Queries.GetParameters;
using Hems_Backend.Application.ParameterValueSettingSchemes.Commands.CreateParameterValueSettingScheme;
using Hems_Backend.Application.ParameterValueSettingSchemes.Commands.DeleteParameterValueSettingScheme;
using Hems_Backend.Application.ParameterValueSettingSchemes.Commands.UpdateParameterValueSettingScheme;
using Hems_Backend.Application.ParameterValueSettingSchemes.Queries.GetParameterValueSettingSchemes;
using Hems_Backend.Application.ParameterValueSettingSchemes.Queries.GetParameterValueSettingSchemesTree;
using Hems_Backend.Domain.Constants;
using Hems_Backend.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Hems_Backend.Web.Endpoints;

/// <summary>
/// Parameter Schemas API - Manages parameter definitions, groups, and model-specific default values
/// This endpoint handles all template/schema operations for defining device parameter structure
/// - Parameter Groups and Parameters: Admin-only access
/// - Value Schemes: GET available to all users, write operations Admin-only
/// </summary>
public class ParameterSchemas : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        // ==== ADMIN ONLY - Parameter Groups and Parameters ====
        app.MapGroup(this)
            .RequireAuthorization(Policies.AdminOnly)

            // PARAMETER GROUPS - Category organization
            .MapGet(GetParameterGroups, "Groups")
            .MapGet(GetParameterGroup, "Groups/{id}")
            .MapPost(CreateParameterGroup, "Groups")
            .MapPut(UpdateParameterGroup, "Groups/{id}")
            .MapDelete(DeleteParameterGroup, "Groups/{id}")

            // PARAMETERS - Parameter definitions
            .MapGet(GetParameters, "Parameters")
            .MapGet(GetParameter, "Parameters/{id}")
            .MapPost(CreateParameter, "Parameters")
            .MapPut(UpdateParameter, "Parameters/{id}")
            .MapDelete(DeleteParameter, "Parameters/{id}")

            // VALUE SCHEMES - Write operations (Admin only)
            .MapPost(CreateParameterValueSettingScheme, "Values")
            .MapPut(UpdateParameterValueSettingScheme, "Values/{id}")
            .MapDelete(DeleteParameterValueSettingScheme, "Values/{id}");

        // ==== ALL AUTHENTICATED USERS - Value Schemes Read Operations ====
        app.MapGroup(this)
            .RequireAuthorization()
            .MapGet(GetParameterValueSettingSchemes, "Values")
            .MapGet(GetParameterValueSettingSchemesTree, "Values/Tree");
    }

    // ========================================================================
    // PARAMETER GROUPS - Organize parameters into hierarchical categories
    // ========================================================================

    /// <summary>
    /// Get all parameter groups
    /// </summary>
    public async Task<ParameterGroupsVm> GetParameterGroups(
        ISender sender)
    {
        return await sender.Send(new GetParameterGroupsQuery());
    }

    /// <summary>
    /// Get a specific parameter group by ID
    /// </summary>
    /// <param name="sender">MediatR sender</param>
    /// <param name="id">Parameter group ID</param>
    /// <param name="includeChildren">Include child groups</param>
    public async Task<ParameterGroupDto?> GetParameterGroup(
        ISender sender,
        Guid id,
        bool includeChildren = false)
    {
        return await sender.Send(new GetParameterGroupQuery { Id = id, IncludeChildren = includeChildren });
    }

    /// <summary>
    /// Create a new parameter group
    /// </summary>
    public async Task<Created<Guid>> CreateParameterGroup(
        ISender sender,
        CreateParameterGroupCommand command)
    {
        var id = await sender.Send(command);
        return TypedResults.Created($"/api/ParameterSchemas/Groups/{id}", id);
    }

    /// <summary>
    /// Update an existing parameter group
    /// </summary>
    public async Task<Results<NoContent, BadRequest>> UpdateParameterGroup(
        ISender sender,
        Guid id,
        UpdateParameterGroupCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();

        await sender.Send(command);
        return TypedResults.NoContent();
    }

    /// <summary>
    /// Delete a parameter group
    /// </summary>
    public async Task<Results<NoContent, BadRequest>> DeleteParameterGroup(
        ISender sender,
        Guid id)
    {
        await sender.Send(new DeleteParameterGroupCommand { Id = id });
        return TypedResults.NoContent();
    }

    // ========================================================================
    // PARAMETERS - Define parameter metadata (name, type, unit, description)
    // ========================================================================

    /// <summary>
    /// Get all parameters, optionally filtered by group
    /// </summary>
    /// <param name="sender">MediatR sender</param>
    /// <param name="groupId">Optional filter by group ID</param>
    public async Task<ParametersVm> GetParameters(
        ISender sender,
        Guid? groupId = null)
    {
        return await sender.Send(new GetParametersQuery { GroupId = groupId });
    }

    /// <summary>
    /// Get a specific parameter by ID
    /// </summary>
    /// <param name="sender">MediatR sender</param>
    /// <param name="id">Parameter ID</param>
    /// <param name="includeGroup">Include parent group information</param>
    public async Task<Hems_Backend.Application.Parameters.Queries.GetParameters.ParameterDto?> GetParameter(
        ISender sender,
        Guid id,
        bool includeGroup = false)
    {
        return await sender.Send(new GetParameterQuery { Id = id, IncludeGroup = includeGroup });
    }

    /// <summary>
    /// Create a new parameter definition
    /// </summary>
    public async Task<Created<Guid>> CreateParameter(
        ISender sender,
        CreateParameterCommand command)
    {
        var id = await sender.Send(command);
        return TypedResults.Created($"/api/ParameterSchemas/Parameters/{id}", id);
    }

    /// <summary>
    /// Update an existing parameter definition
    /// </summary>
    public async Task<Results<NoContent, BadRequest>> UpdateParameter(
        ISender sender,
        Guid id,
        UpdateParameterCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();

        await sender.Send(command);
        return TypedResults.NoContent();
    }

    /// <summary>
    /// Delete a parameter definition
    /// </summary>
    public async Task<Results<NoContent, BadRequest>> DeleteParameter(
        ISender sender,
        Guid id)
    {
        await sender.Send(new DeleteParameterCommand { Id = id });
        return TypedResults.NoContent();
    }

    // ========================================================================
    // VALUE SCHEMES - Model-specific default values (Templates)
    // ========================================================================

    /// <summary>
    /// Get parameter value setting schemes, optionally filtered by model number
    /// These are the template default values for each device model
    /// </summary>
    /// <param name="sender">MediatR sender</param>
    /// <param name="modelNumber">Optional filter by device model</param>
    public async Task<ParameterValueSettingSchemesVm> GetParameterValueSettingSchemes(
        ISender sender,
        string? modelNumber = null)
    {
        return await sender.Send(new GetParameterValueSettingSchemesQuery { ModelNumber = modelNumber });
    }

    /// <summary>
    /// Get parameter value setting schemes in tree structure
    /// Shows the hierarchical organization of parameters with their default values
    /// </summary>
    /// <param name="sender">MediatR sender</param>
    /// <param name="modelNumber">Optional filter by device model</param>
    public async Task<ParameterValueSettingSchemesTreeVm> GetParameterValueSettingSchemesTree(
        ISender sender,
        string? modelNumber = null)
    {
        return await sender.Send(new GetParameterValueSettingSchemesTreeQuery { ModelNumber = modelNumber });
    }

    /// <summary>
    /// Create a new parameter value setting scheme
    /// Sets the default value for a parameter for a specific device model
    /// </summary>
    public async Task<Created<Guid>> CreateParameterValueSettingScheme(
        ISender sender,
        CreateParameterValueSettingSchemeCommand command)
    {
        var id = await sender.Send(command);
        return TypedResults.Created($"/api/ParameterSchemas/Values/{id}", id);
    }

    /// <summary>
    /// Update an existing parameter value setting scheme
    /// Changes the default value for a parameter for a specific device model
    /// </summary>
    public async Task<Results<NoContent, BadRequest>> UpdateParameterValueSettingScheme(
        ISender sender,
        Guid id,
        UpdateParameterValueSettingSchemeCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();

        await sender.Send(command);
        return TypedResults.NoContent();
    }

    /// <summary>
    /// Delete a parameter value setting scheme
    /// </summary>
    public async Task<Results<NoContent, BadRequest>> DeleteParameterValueSettingScheme(
        ISender sender,
        Guid id)
    {
        await sender.Send(new DeleteParameterValueSettingSchemeCommand { Id = id });
        return TypedResults.NoContent();
    }
}
