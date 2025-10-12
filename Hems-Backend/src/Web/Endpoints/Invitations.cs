using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.Invitations.Commands.AcceptInvitation;
using Hems_Backend.Application.Invitations.Commands.CreateInvitation;
using Hems_Backend.Application.Invitations.Commands.DeclineInvitation;
using Hems_Backend.Application.Invitations.Commands.MarkNotificationAsRead;
using Hems_Backend.Application.Invitations.Queries.GetInvitationsBySite;
using Hems_Backend.Application.Invitations.Queries.GetUserHomeSites;
using Hems_Backend.Application.Invitations.Queries.GetUserNotifications;
using Microsoft.AspNetCore.Authorization;

namespace Hems_Backend.Web.Endpoints;

public class Invitations : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .RequireAuthorization()
            // Davetiye gönderme
            .MapPost(CreateInvitation, "create")
            // Davetiye listeleme (belirli bir site için)
            .MapGet(GetInvitationsBySite, "by-site/{siteId}")
            // Davetiye kabul etme
            .MapPost(AcceptInvitation, "accept")
            // Davetiye reddetme
            .MapPost(DeclineInvitation, "decline")
            // Kullanıcının evlerini listeleme (sahibi olduğu + paylaşılan)
            .MapGet(GetUserHomeSites, "user-home-sites")
            // Kullanıcının bildirimlerini getirme
            .MapGet(GetUserNotifications, "notifications")
            // Bildirimi okundu olarak işaretleme
            .MapPut(MarkNotificationAsRead, "notifications/{notificationId}/mark-read");
    }

    /// <summary>
    /// AŞAMA 1: Davetiye Gönderimi
    /// </summary>
    [Authorize]
    public async Task<Guid> CreateInvitation(
        ISender sender,
        CreateInvitationCommand command)
    {
        return await sender.Send(command);
    }

    /// <summary>
    /// AŞAMA 2: Davetiye Listeleme
    /// </summary>
    [Authorize]
    public async Task<InvitationsVm> GetInvitationsBySite(
        ISender sender,
        Guid siteId)
    {
        return await sender.Send(new GetInvitationsBySiteQuery { SiteId = siteId });
    }

    /// <summary>
    /// AŞAMA 3: Davetiye Kabul Etme
    /// </summary>
    [Authorize]
    public async Task<IResult> AcceptInvitation(
        ISender sender,
        AcceptInvitationCommand command)
    {
        await sender.Send(command);
        return Results.Ok(new { message = "Davetiye başarıyla kabul edildi." });
    }

    /// <summary>
    /// AŞAMA 3: Davetiye Reddetme
    /// </summary>
    [Authorize]
    public async Task<IResult> DeclineInvitation(
        ISender sender,
        DeclineInvitationCommand command)
    {
        await sender.Send(command);
        return Results.Ok(new { message = "Davetiye reddedildi." });
    }

    /// <summary>
    /// AŞAMA 4: Kullanıcının Evlerini Listeleme
    /// </summary>
    [Authorize]
    public async Task<UserHomeSitesVm> GetUserHomeSites(
        ISender sender)
    {
        return await sender.Send(new GetUserHomeSitesQuery());
    }

    /// <summary>
    /// Kullanıcının Bildirimlerini Getirme
    /// </summary>
    [Authorize]
    public async Task<List<NotificationDto>> GetUserNotifications(
        ISender sender)
    {
        return await sender.Send(new GetUserNotificationsQuery());
    }

    /// <summary>
    /// Bildirimi Okundu Olarak İşaretleme
    /// </summary>
    [Authorize]
    public async Task<IResult> MarkNotificationAsRead(
        ISender sender,
        Guid notificationId)
    {
        await sender.Send(new MarkNotificationAsReadCommand { NotificationId = notificationId });
        return Results.NoContent();
    }
}

