using Hems_Backend.Application.RoleManagement.Commands.CreateRole;
using Hems_Backend.Application.RoleManagement.Commands.DeleteRole;
using Hems_Backend.Application.RoleManagement.Commands.AssignRoleToUser;
using Hems_Backend.Application.RoleManagement.Queries.GetRoles;
using Hems_Backend.Application.RoleManagement.Queries.GetUserRoles;
using Hems_Backend.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Hems_Backend.Web.Endpoints;

/// <summary>
/// Role Management endpoints - OBSOLETE: These endpoints are not in use by any client application.
/// Scheduled for removal in a future release.
/// </summary>
[Obsolete("RoleManagement endpoints are not in use and scheduled for deletion. Do not use these endpoints.")]
public class RoleManagement : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        #pragma warning disable CS0618 // Suppress obsolete warnings for endpoint mapping
        app.MapGroup(this)
            .RequireAuthorization(Policies.AdminOnly)
            .MapGet(GetRoles)
            .MapPost(CreateRole)
            .MapDelete(DeleteRole, "{roleName}")
            .MapPut(AssignRoleToUser, "assign")
            .MapGet(GetUserRoles, "user/{userId}");
        #pragma warning restore CS0618
    }

    [Obsolete("This endpoint is not in use and scheduled for deletion.")]
    public async Task<Ok<RolesVm>> GetRoles(ISender sender)
    {
        var vm = await sender.Send(new GetRolesQuery());
        return TypedResults.Ok(vm);
    }

    [Obsolete("This endpoint is not in use and scheduled for deletion.")]
    public async Task<Results<Ok, BadRequest>> CreateRole(ISender sender, CreateRoleCommand command)
    {
        var result = await sender.Send(command);

        if (result.Succeeded)
        {
            return TypedResults.Ok();
        }

        return TypedResults.BadRequest();
    }

    [Obsolete("This endpoint is not in use and scheduled for deletion.")]
    public async Task<Results<NoContent, BadRequest>> DeleteRole(ISender sender, string roleName)
    {
        var result = await sender.Send(new DeleteRoleCommand(roleName));

        if (result.Succeeded)
        {
            return TypedResults.NoContent();
        }

        return TypedResults.BadRequest();
    }

    [Obsolete("This endpoint is not in use and scheduled for deletion.")]
    public async Task<Results<Ok, BadRequest>> AssignRoleToUser(ISender sender, AssignRoleToUserCommand command)
    {
        var result = await sender.Send(command);

        if (result.Succeeded)
        {
            return TypedResults.Ok();
        }

        return TypedResults.BadRequest();
    }

    [Obsolete("This endpoint is not in use and scheduled for deletion.")]
    public async Task<Ok<UserRolesVm>> GetUserRoles(ISender sender, string userId)
    {
        var vm = await sender.Send(new GetUserRolesQuery(userId));
        return TypedResults.Ok(vm);
    }
}
