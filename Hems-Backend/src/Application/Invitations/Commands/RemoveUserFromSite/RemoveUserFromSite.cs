using Hems_Backend.Application.Common.Exceptions;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Invitations.Commands.RemoveUserFromSite;

public record RemoveUserFromSiteCommand : IRequest<Unit>
{
    public Guid InvitationId { get; init; }
}

public class RemoveUserFromSiteCommandHandler : IRequestHandler<RemoveUserFromSiteCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public RemoveUserFromSiteCommandHandler(
        IApplicationDbContext context,
        IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<Unit> Handle(RemoveUserFromSiteCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _user.Id ?? throw new UnauthorizedAccessException("User not authenticated");

        // Get invitation
        var invitation = await _context.Invitations
            .FirstOrDefaultAsync(i => i.Id == request.InvitationId, cancellationToken)
            ?? throw new Common.Exceptions.NotFoundException(nameof(Invitation), request.InvitationId);

        // Verify site exists and current user is the owner
        var site = await _context.Sites
            .FirstOrDefaultAsync(s => s.Id == invitation.SiteId && 
                                     !s.IsDeleted && 
                                     s.UserId == currentUserId, 
                                 cancellationToken)
            ?? throw new Common.Exceptions.NotFoundException(nameof(Site), invitation.SiteId);

        // If InvitedUserId exists, check and delete UserSite record
        if (!string.IsNullOrEmpty(invitation.InvitedUserId))
        {
            var userSite = await _context.UserSites
                .FirstOrDefaultAsync(us => us.SiteId == invitation.SiteId && 
                                         us.UserId == invitation.InvitedUserId, 
                                     cancellationToken);

            if (userSite != null)
            {
                _context.UserSites.Remove(userSite);
            }
        }

        // Delete invitation
        _context.Invitations.Remove(invitation);

        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

