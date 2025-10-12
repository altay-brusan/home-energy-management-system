using Hems_Backend.Application.Common.Exceptions;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Invitations.Commands.MarkNotificationAsRead;

public record MarkNotificationAsReadCommand : IRequest<Unit>
{
    public Guid NotificationId { get; init; }
}

public class MarkNotificationAsReadCommandHandler : IRequestHandler<MarkNotificationAsReadCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public MarkNotificationAsReadCommandHandler(
        IApplicationDbContext context,
        IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<Unit> Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _user.Id ?? throw new UnauthorizedAccessException("User not authenticated");

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == request.NotificationId && n.UserId == currentUserId, cancellationToken)
            ?? throw new Common.Exceptions.NotFoundException(nameof(Notification), request.NotificationId);

        notification.IsRead = true;

        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

