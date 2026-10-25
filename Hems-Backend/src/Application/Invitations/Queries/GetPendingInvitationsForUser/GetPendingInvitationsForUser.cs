using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Invitations.Queries.GetPendingInvitationsForUser;

public record GetPendingInvitationsForUserQuery : IRequest<PendingInvitationsResult>;

public class PendingInvitationsResult
{
    public bool HasPendingInvitations { get; set; }
    public List<PendingInvitationDto> Invitations { get; set; } = new();
}

public class PendingInvitationDto
{
    public Guid InvitationId { get; set; }
    public string SiteName { get; set; } = string.Empty;
    public string InvitedByEmail { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsExpired { get; set; }
}

public class GetPendingInvitationsForUserQueryHandler : IRequestHandler<GetPendingInvitationsForUserQuery, PendingInvitationsResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IIdentityService _identityService;

    // Invitation expires after 24 hours
    private const int InvitationExpirationHours = 24;

    public GetPendingInvitationsForUserQueryHandler(
        IApplicationDbContext context,
        IUser user,
        IIdentityService identityService)
    {
        _context = context;
        _user = user;
        _identityService = identityService;
    }

    public async Task<PendingInvitationsResult> Handle(GetPendingInvitationsForUserQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _user.Id ?? throw new UnauthorizedAccessException("User not authenticated");

        // Get current user's email
        var userEmail = await _identityService.GetUserEmailAsync(currentUserId);
        if (string.IsNullOrEmpty(userEmail))
        {
            return new PendingInvitationsResult { HasPendingInvitations = false };
        }

        var now = DateTimeOffset.UtcNow;
        var expirationThreshold = now.AddHours(-InvitationExpirationHours);

        // Find pending invitations for this email
        var pendingInvitations = await _context.Invitations
            .Include(i => i.Site)
            .Where(i => i.Email.ToLower() == userEmail.ToLower()
                     && i.Status == InvitationStatus.Pending
                     && !i.Site.IsDeleted)
            .OrderByDescending(i => i.Created)
            .Select(i => new PendingInvitationDto
            {
                InvitationId = i.Id,
                SiteName = i.Site.Name,
                InvitedByEmail = i.InvitedByUserId, // We'll resolve this to email below
                Token = i.Token,
                CreatedAt = i.Created,
                IsExpired = i.Created < expirationThreshold
            })
            .ToListAsync(cancellationToken);

        // Resolve InvitedByUserId to email for each invitation
        foreach (var invitation in pendingInvitations)
        {
            var inviterEmail = await _identityService.GetUserEmailAsync(invitation.InvitedByEmail);
            invitation.InvitedByEmail = inviterEmail ?? "Unknown";
        }

        // Filter out expired invitations for the result
        var validInvitations = pendingInvitations.Where(i => !i.IsExpired).ToList();

        return new PendingInvitationsResult
        {
            HasPendingInvitations = validInvitations.Any(),
            Invitations = validInvitations
        };
    }
}
