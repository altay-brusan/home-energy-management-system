using Hems_Backend.Application.Auth.Queries.GetUserStatus;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;

namespace Hems_Backend.Web.Endpoints;

/// <summary>
/// Auth endpoints - OBSOLETE: These endpoints are not in use by any client application.
/// </summary>
[Obsolete("Auth endpoints are not in use and scheduled for deletion.")]
public class Auth : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        #pragma warning disable CS0618 // Suppress obsolete warning for endpoint mapping
        app.MapGroup(this)
            .RequireAuthorization()
            .MapGet(GetUserStatus, "user-status")
            .MapGet(GetUserStatusMobile, "mobile/get-user-status");
        #pragma warning restore CS0618
    }

    [Authorize]
    [Obsolete("This endpoint is not in use and scheduled for deletion.")]
    public async Task<UserStatusDto> GetUserStatus(
        ISender sender)
    {
        return await sender.Send(new GetUserStatusQuery());
    }

    // Mobile endpoint
    [Authorize]
    [Obsolete("This endpoint is not in use and scheduled for deletion.")]
    public async Task<UserStatusDto> GetUserStatusMobile(
        ISender sender)
    {
        return await sender.Send(new GetUserStatusQuery());
    }
}
