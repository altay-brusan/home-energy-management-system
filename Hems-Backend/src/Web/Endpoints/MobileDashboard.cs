using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.Mobile.Queries.GetMobileDashboard;
using Hems_Backend.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;

namespace Hems_Backend.Web.Endpoints.Mobile;

public class MobileDashboard : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .RequireAuthorization()
            .MapGet(GetDashboard, "mobile/get-dashboard");
    }

    [Authorize]
    public async Task<MobileDashboardDto> GetDashboard(
        ISender sender)
    {
        return await sender.Send(new GetMobileDashboardQuery());
    }
}
