using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.Invitations.Commands.AcceptInvitation;
using Hems_Backend.Application.Invitations.Commands.CreateInvitation;
using Hems_Backend.Application.Invitations.Commands.DeclineInvitation;
using Hems_Backend.Application.Invitations.Commands.RemoveUserFromSite;
using Hems_Backend.Application.Invitations.Queries.GetInvitationsBySite;
using Hems_Backend.Application.Invitations.Queries.GetPendingInvitationsForUser;
using Microsoft.AspNetCore.Authorization;

namespace Hems_Backend.Web.Endpoints;

public class Invitations : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .RequireAuthorization()
            .MapPost(CreateInvitation, "Create")
            .MapGet(GetInvitationsBySite, "BySite/{siteId}")
            .MapGet(GetPendingInvitationsForUser, "Pending")
            .MapPost(AcceptInvitation, "Accept")
            .MapPost(DeclineInvitation, "Decline")
            .MapDelete(RemoveUserFromSite, "RemoveUser/{invitationId}");
    }

    /// <summary>
    /// Create a new invitation to share a site with another user.
    /// </summary>
    [Authorize]
    public async Task<Guid> CreateInvitation(
        ISender sender,
        CreateInvitationCommand command)
    {
        return await sender.Send(command);
    }

    /// <summary>
    /// Get all invitations for a specific site.
    /// </summary>
    [Authorize]
    public async Task<InvitationsVm> GetInvitationsBySite(
        ISender sender,
        Guid siteId)
    {
        return await sender.Send(new GetInvitationsBySiteQuery { SiteId = siteId });
    }

    /// <summary>
    /// Get pending invitations for the current user (by email).
    /// Used to check if a new user has pending invitations before forcing home creation.
    /// </summary>
    [Authorize]
    public async Task<PendingInvitationsResult> GetPendingInvitationsForUser(
        ISender sender)
    {
        return await sender.Send(new GetPendingInvitationsForUserQuery());
    }

    /// <summary>
    /// Accept a pending invitation.
    /// </summary>
    [Authorize]
    public async Task<IResult> AcceptInvitation(
        ISender sender,
        AcceptInvitationCommand command)
    {
        await sender.Send(command);
        return Results.Ok(new { message = "Invitation accepted successfully." });
    }

    /// <summary>
    /// Decline a pending invitation.
    /// </summary>
    [Authorize]
    public async Task<IResult> DeclineInvitation(
        ISender sender,
        DeclineInvitationCommand command)
    {
        await sender.Send(command);
        return Results.Ok(new { message = "Invitation declined." });
    }

    /// <summary>
    /// Remove a user from a shared site.
    /// </summary>
    [Authorize]
    public async Task<IResult> RemoveUserFromSite(
        ISender sender,
        Guid invitationId)
    {
        await sender.Send(new RemoveUserFromSiteCommand { InvitationId = invitationId });
        return Results.Ok(new { message = "User removed from site successfully." });
    }
}

