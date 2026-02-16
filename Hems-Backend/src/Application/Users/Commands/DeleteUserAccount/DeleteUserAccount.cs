using Hems_Backend.Application.Common.Exceptions;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Users.Commands.DeleteUserAccount;

public record DeleteUserAccountCommand : IRequest;

public class DeleteUserAccountCommandHandler : IRequestHandler<DeleteUserAccountCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IIdentityService _identityService;

    public DeleteUserAccountCommandHandler(
        IApplicationDbContext context,
        IUser user,
        IIdentityService identityService)
    {
        _context = context;
        _user = user;
        _identityService = identityService;
    }

    public async Task Handle(DeleteUserAccountCommand request, CancellationToken cancellationToken)
    {
        var userId = _user.Id
            ?? throw AppException.Unauthorized(MessageCodes.AUTH_USER_NOT_AUTHENTICATED);

        // Block demo users from deleting their account
        var isDemo = await _identityService.IsUserDemoAsync(userId, cancellationToken);
        if (isDemo)
        {
            throw AppException.BadRequest(MessageCodes.USER_DEMO_CANNOT_DELETE);
        }

        // Load all owned non-deleted sites
        var ownedSites = await _context.Sites
            .Where(s => s.UserId == userId && !s.IsDeleted)
            .ToListAsync(cancellationToken);

        // Check if any owned site has active guest UserSite records
        var ownedSiteIds = ownedSites.Select(s => s.Id).ToList();

        if (ownedSiteIds.Count > 0)
        {
            var sharedSiteCount = await _context.UserSites
                .Where(us => ownedSiteIds.Contains(us.SiteId) && us.UserId != userId)
                .Select(us => us.SiteId)
                .Distinct()
                .CountAsync(cancellationToken);

            if (sharedSiteCount > 0)
            {
                throw AppException.BadRequest(
                    MessageCodes.USER_CANNOT_DELETE_WITH_ACTIVE_SHARES,
                    new Dictionary<string, object> { ["sharedSiteCount"] = sharedSiteCount });
            }
        }

        // Clean up owned sites: hard-delete Setup/SetupDevice chain, soft-delete Site
        foreach (var site in ownedSites)
        {
            var setup = await _context.Setups
                .Include(s => s.SetupDevice)
                .FirstOrDefaultAsync(s => s.SiteId == site.Id, cancellationToken);

            if (setup != null)
            {
                if (setup.SetupDevice != null)
                {
                    _context.SetupDevices.Remove(setup.SetupDevice);
                }

                _context.Setups.Remove(setup);
            }

            // Remove invitations for this site (prevent stale invitation accepts)
            var siteInvitations = await _context.Invitations
                .Where(i => i.SiteId == site.Id)
                .ToListAsync(cancellationToken);
            _context.Invitations.RemoveRange(siteInvitations);

            // Soft-delete the site
            site.IsDeleted = true;
        }

        // Remove all UserSite records where this user is a member (guest-on-other-sites + own sites)
        var userSites = await _context.UserSites
            .Where(us => us.UserId == userId)
            .ToListAsync(cancellationToken);
        _context.UserSites.RemoveRange(userSites);

        // Remove all invitations where this user is the invited user (received invitations)
        var receivedInvitations = await _context.Invitations
            .Where(i => i.InvitedUserId == userId)
            .ToListAsync(cancellationToken);
        _context.Invitations.RemoveRange(receivedInvitations);

        // Remove orphaned notifications for this user
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId)
            .ToListAsync(cancellationToken);
        _context.Notifications.RemoveRange(notifications);

        // Soft-delete the user account (also invalidates tokens via security stamp update)
        var result = await _identityService.SoftDeleteUserAsync(userId, cancellationToken);
        if (!result.Succeeded)
        {
            throw AppException.BadRequest(
                MessageCodes.INTERNAL_ERROR,
                new Dictionary<string, object> { ["errors"] = result.Errors });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
