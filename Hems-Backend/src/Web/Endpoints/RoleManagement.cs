using Hems_Backend.Application.RoleManagement.Commands.CreateRole;
using Hems_Backend.Application.RoleManagement.Commands.DeleteRole;
using Hems_Backend.Application.RoleManagement.Commands.AssignRoleToUser;
using Hems_Backend.Application.RoleManagement.Queries.GetRoles;
using Hems_Backend.Application.RoleManagement.Queries.GetUserRoles;
using Hems_Backend.Domain.Constants;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Hems_Backend.Web.Endpoints;

/// <summary>
/// Role Management endpoints - Admin-only access for managing roles and user role assignments.
/// </summary>
public class RoleManagement : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .RequireAuthorization(Policies.AdminOnly)
            .MapGet(GetRoles, "Admin/Roles")
            .MapPost(CreateRole, "Admin/Roles")
            .MapDelete(DeleteRole, "Admin/Roles/{roleName}")
            .MapPut(AssignRoleToUser, "Admin/Assign")
            .MapGet(GetUserRoles, "Admin/User/{userId}/Roles");
    }

    /// <summary>
    /// Get all roles in the system.
    /// </summary>
    public async Task<Ok<RolesVm>> GetRoles(ISender sender)
    {
        var vm = await sender.Send(new GetRolesQuery());
        return TypedResults.Ok(vm);
    }

    /// <summary>
    /// Create a new role.
    /// </summary>
    public async Task<Results<Ok, BadRequest>> CreateRole(ISender sender, CreateRoleCommand command)
    {
        var result = await sender.Send(command);

        if (result.Succeeded)
        {
            return TypedResults.Ok();
        }

        return TypedResults.BadRequest();
    }

    /// <summary>
    /// Delete a role by name.
    /// </summary>
    public async Task<Results<NoContent, BadRequest>> DeleteRole(ISender sender, string roleName)
    {
        var result = await sender.Send(new DeleteRoleCommand(roleName));

        if (result.Succeeded)
        {
            return TypedResults.NoContent();
        }

        return TypedResults.BadRequest();
    }

    /// <summary>
    /// Assign a role to a user.
    /// </summary>
    public async Task<Results<Ok, BadRequest>> AssignRoleToUser(ISender sender, AssignRoleToUserCommand command)
    {
        var result = await sender.Send(command);

        if (result.Succeeded)
        {
            return TypedResults.Ok();
        }

        return TypedResults.BadRequest();
    }

    /// <summary>
    /// Get all roles assigned to a user.
    /// </summary>
    public async Task<Ok<UserRolesVm>> GetUserRoles(ISender sender, string userId)
    {
        var vm = await sender.Send(new GetUserRolesQuery(userId));
        return TypedResults.Ok(vm);
    }
}
