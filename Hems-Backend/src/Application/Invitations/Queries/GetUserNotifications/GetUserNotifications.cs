using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Invitations.Queries.GetUserNotifications;

public record GetUserNotificationsQuery : IRequest<List<NotificationDto>>;

public class GetUserNotificationsQueryHandler : IRequestHandler<GetUserNotificationsQuery, List<NotificationDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetUserNotificationsQueryHandler(
        IApplicationDbContext context,
        IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<List<NotificationDto>> Handle(GetUserNotificationsQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _user.Id ?? throw new UnauthorizedAccessException("User not authenticated");

        var notifications = await _context.Notifications
            .Where(n => n.UserId == currentUserId)
            .OrderByDescending(n => n.Created)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Message = n.Message,
                IsRead = n.IsRead,
                InvitationId = n.InvitationId,
                Created = n.Created
            })
            .ToListAsync(cancellationToken);

        return notifications;
    }
}

