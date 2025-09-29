using Hems_Backend.Infrastructure.Identity;
using Hems_Backend.Web.Infrastructure;
using Microsoft.AspNetCore.Identity;

namespace Hems_Backend.Web.Endpoints;

public class Users : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        // Grubu tanımla
        var group = app.MapGroup(this);

        // Identity API'yi ekle
        group.MapIdentityApiOklidis<ApplicationUser>();


      
    }
}
