using Hems_Backend.Application.Common.Exceptions;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Entities;
using Hems_Backend.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Invitations.Commands.AcceptInvitation;

public record AcceptInvitationCommand : IRequest<Unit>
{
    public string Token { get; init; } = string.Empty;
}

public class AcceptInvitationCommandHandler : IRequestHandler<AcceptInvitationCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IIdentityService _identityService;
    private readonly TimeProvider _timeProvider;

    public AcceptInvitationCommandHandler(
        IApplicationDbContext context,
        IUser user,
        IIdentityService identityService,
        TimeProvider timeProvider)
    {
        _context = context;
        _user = user;
        _identityService = identityService;
        _timeProvider = timeProvider;
    }

    public async Task<Unit> Handle(AcceptInvitationCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _user.Id ?? throw new UnauthorizedAccessException("User not authenticated");
        
        // Get current user's email
        var currentUserEmail = await _identityService.GetUserEmailAsync(currentUserId)
            ?? throw new UnauthorizedAccessException("User email address not found");

        var utcNow = _timeProvider.GetUtcNow();
        var twentyFourHoursAgo = utcNow.AddHours(-24);

        // Find invitation by token
        var invitation = await _context.Invitations
            .FirstOrDefaultAsync(i =>
                i.Token == request.Token &&
                i.Status == InvitationStatus.Pending &&
                (i.Email == currentUserEmail || i.InvitedUserId == currentUserId),
                cancellationToken)
            ?? throw new Common.Exceptions.NotFoundException(nameof(Invitation), request.Token);

        // Check if invitation has expired (24 hours)
        if (invitation.Created < twentyFourHoursAgo)
        {
            invitation.Status = InvitationStatus.Expired;
            await _context.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("This invitation has expired. Please request a new invitation.");
        }

        // Update invitation
        invitation.Status = InvitationStatus.Accepted;
        invitation.AcceptanceTime = utcNow;

        // If InvitedUserId is empty, update it now
        if (string.IsNullOrEmpty(invitation.InvitedUserId))
        {
            invitation.InvitedUserId = currentUserId;
        }

        // Create UserSite record (check first, throw error if already exists)
        var existingUserSite = await _context.UserSites
            .FirstOrDefaultAsync(us => us.UserId == currentUserId && us.SiteId == invitation.SiteId, cancellationToken);

        if (existingUserSite != null)
        {
            throw new InvalidOperationException("This site is already shared with you.");
        }

        var userSite = new UserSite
        {
            Id = Guid.NewGuid(),
            UserId = currentUserId,
            SiteId = invitation.SiteId,
            Role = UserSiteRole.Guest,
            Created = utcNow
        };

        _context.UserSites.Add(userSite);

        // Update notification
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.InvitationId == invitation.Id, cancellationToken);

        if (notification != null && string.IsNullOrEmpty(notification.UserId))
        {
            notification.UserId = currentUserId;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

