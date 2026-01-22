using Hems_Backend.Application.Common.Exceptions;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Domain.Entities;
using Hems_Backend.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Invitations.Commands.CreateInvitation;

public record CreateInvitationCommand : IRequest<Guid>
{
    public string Email { get; init; } = string.Empty;
    public Guid SiteId { get; init; }
}

public class CreateInvitationCommandHandler : IRequestHandler<CreateInvitationCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IIdentityService _identityService;
    private readonly IInvitationEmailService _emailService;
    private readonly TimeProvider _timeProvider;

    public CreateInvitationCommandHandler(
        IApplicationDbContext context,
        IUser user,
        IIdentityService identityService,
        IInvitationEmailService emailService,
        TimeProvider timeProvider)
    {
        _context = context;
        _user = user;
        _identityService = identityService;
        _emailService = emailService;
        _timeProvider = timeProvider;
    }

    public async Task<Guid> Handle(CreateInvitationCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _user.Id
            ?? throw AppException.Unauthorized(MessageCodes.AUTH_USER_NOT_AUTHENTICATED);

        // Verify site exists
        var site = await _context.Sites
            .FirstOrDefaultAsync(s => s.Id == request.SiteId && !s.IsDeleted, cancellationToken)
            ?? throw AppException.NotFound(
                MessageCodes.SITE_NOT_FOUND,
                new Dictionary<string, object> { ["siteId"] = request.SiteId });

        // Only site owner can send invitations
        if (site.UserId != currentUserId)
        {
            throw AppException.Forbidden(
                MessageCodes.INVITATION_ONLY_OWNER_CAN_INVITE,
                new Dictionary<string, object> { ["siteId"] = request.SiteId });
        }

        // Look up user by email
        var invitedUserId = await _identityService.GetUserIdByEmailAsync(request.Email);

        // Check if there's already a pending invitation for this user and site
        var existingInvitation = await _context.Invitations
            .FirstOrDefaultAsync(i =>
                i.Email == request.Email &&
                i.SiteId == request.SiteId &&
                i.Status == InvitationStatus.Pending,
                cancellationToken);

        if (existingInvitation != null)
        {
            throw AppException.BadRequest(
                MessageCodes.INVITATION_ALREADY_EXISTS,
                new Dictionary<string, object> { ["email"] = request.Email, ["siteId"] = request.SiteId });
        }

        // Check if user is trying to invite themselves
        if (!string.IsNullOrEmpty(invitedUserId) && invitedUserId == currentUserId)
        {
            throw AppException.BadRequest(MessageCodes.INVITATION_CANNOT_INVITE_SELF);
        }

        // Check if user is the site owner (can't invite yourself)
        if (site.UserId == invitedUserId && !string.IsNullOrEmpty(invitedUserId))
        {
            throw AppException.BadRequest(
                MessageCodes.INVITATION_CANNOT_INVITE_OWNER,
                new Dictionary<string, object> { ["email"] = request.Email });
        }

        // Check if user is already a member of the site (prevent spam invitations)
        if (!string.IsNullOrEmpty(invitedUserId))
        {
            var existingMembership = await _context.UserSites
                .AnyAsync(us => us.UserId == invitedUserId && us.SiteId == request.SiteId, cancellationToken);

            if (existingMembership)
            {
                throw AppException.BadRequest(
                    MessageCodes.INVITATION_USER_ALREADY_MEMBER,
                    new Dictionary<string, object> { ["email"] = request.Email, ["siteId"] = request.SiteId });
            }
        }

        // Generate token (6-digit random number)
        var token = await GenerateUniqueTokenAsync(cancellationToken);

        var utcNow = _timeProvider.GetUtcNow();

        // Create invitation record
        var invitation = new Invitation
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            InvitedUserId = invitedUserId,
            InvitedByUserId = currentUserId,
            SiteId = request.SiteId,
            Token = token,
            Status = InvitationStatus.Pending,
            Created = utcNow
        };

        _context.Invitations.Add(invitation);

        // Create notification record
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            UserId = invitedUserId,
            InvitationId = invitation.Id,
            Message = $"You have been invited to share site '{site.Name}'.",
            IsRead = false,
            Created = utcNow
        };

        _context.Notifications.Add(notification);

        await _context.SaveChangesAsync(cancellationToken);

        // Send invitation email
        var currentUserName = await _identityService.GetUserNameAsync(currentUserId);
        await _emailService.SendInvitationEmailAsync(
            request.Email,
            currentUserName ?? "User",
            site.Name,
            token,
            cancellationToken);

        return invitation.Id;
    }

    private async Task<string> GenerateUniqueTokenAsync(CancellationToken cancellationToken)
    {
        var random = new Random();
        var twentyFourHoursAgo = _timeProvider.GetUtcNow().AddHours(-24);

        string token;
        bool isUnique;

        do
        {
            // Generate 6-digit random number
            token = random.Next(100000, 999999).ToString();

            // Check if token was used in the last 24 hours
            isUnique = !await _context.Invitations
                .AnyAsync(i => i.Token == token && i.Created > twentyFourHoursAgo, cancellationToken);

        } while (!isUnique);

        return token;
    }
}
