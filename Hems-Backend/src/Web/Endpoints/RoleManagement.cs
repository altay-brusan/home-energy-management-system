using Hems_Backend.Application.RoleManagement.Commands.CreateRole;
using Hems_Backend.Application.RoleManagement.Commands.DeleteRole;
using Hems_Backend.Application.RoleManagement.Commands.AssignRoleToUser;
using Hems_Backend.Application.RoleManagement.Queries.GetRoles;
using Hems_Backend.Application.RoleManagement.Queries.GetUserRoles;
using Hems_Backend.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Hems_Backend.Web.Endpoints;

public class RoleManagement : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .RequireAuthorization(Policies.AdminOnly)
            .MapGet(GetRoles)
            .MapPost(CreateRole)
            .MapDelete(DeleteRole, "{roleName}")
            .MapPut(AssignRoleToUser, "assign")
            .MapGet(GetUserRoles, "user/{userId}");
    }

    public async Task<Ok<RolesVm>> GetRoles(ISender sender)
    {
        var vm = await sender.Send(new GetRolesQuery());
        return TypedResults.Ok(vm);
    }

    public async Task<Results<Ok, BadRequest>> CreateRole(ISender sender, CreateRoleCommand command)
    {
        var result = await sender.Send(command);
        
        if (result.Succeeded)
        {
            return TypedResults.Ok();
        }
        
        return TypedResults.BadRequest();
    }

    public async Task<Results<NoContent, BadRequest>> DeleteRole(ISender sender, string roleName)
    {
        var result = await sender.Send(new DeleteRoleCommand(roleName));
        
        if (result.Succeeded)
        {
            return TypedResults.NoContent();
        }
        
        return TypedResults.BadRequest();
    }

    public async Task<Results<Ok, BadRequest>> AssignRoleToUser(ISender sender, AssignRoleToUserCommand command)
    {
        var result = await sender.Send(command);
        
        if (result.Succeeded)
        {
            return TypedResults.Ok();
        }
        
        return TypedResults.BadRequest();
    }

    public async Task<Ok<UserRolesVm>> GetUserRoles(ISender sender, string userId)
    {
        var vm = await sender.Send(new GetUserRolesQuery(userId));
        return TypedResults.Ok(vm);
    }
}
