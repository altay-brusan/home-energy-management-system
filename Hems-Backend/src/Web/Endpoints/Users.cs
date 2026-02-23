using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Users.Commands.DeleteUserAccount;
using Hems_Backend.Application.Users.Commands.UpdateUser;
using Hems_Backend.Infrastructure.Identity;
using Hems_Backend.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;
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

        // Kullanıcı kendi profilini güncelleme endpointi
        group.MapPut(UpdateCurrentUser, "update-profile");

        // Kullanıcı hesabını silme endpointi
        group.MapDelete(DeleteAccount, "delete-account");
    }

    [Authorize]
    public static async Task<IResult> UpdateCurrentUser(UpdateUserCommand command, ISender sender)
    {
        var result = await sender.Send(command);

        return result.Succeeded ? Results.NoContent() : Results.BadRequest(result.Errors);
    }

    [Authorize]
    public static async Task<IResult> DeleteAccount(ISender sender)
    {
        await sender.Send(new DeleteUserAccountCommand());

        return Results.NoContent();
    }
}
