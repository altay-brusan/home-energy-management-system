using Hems_Backend.Application.Auth.Queries.GetUserStatus;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;

namespace Hems_Backend.Web.Endpoints;

public class Auth : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .RequireAuthorization()
            .MapGet(GetUserStatus, "user-status")
            .MapGet(GetUserStatusMobile, "mobile/get-user-status");
    }

    [Authorize]
    public async Task<UserStatusDto> GetUserStatus(
        ISender sender)
    {
        return await sender.Send(new GetUserStatusQuery());
    }

    // Mobile endpoint
    [Authorize]
    public async Task<UserStatusDto> GetUserStatusMobile(
        ISender sender)
    {
        return await sender.Send(new GetUserStatusQuery());
    }
}
