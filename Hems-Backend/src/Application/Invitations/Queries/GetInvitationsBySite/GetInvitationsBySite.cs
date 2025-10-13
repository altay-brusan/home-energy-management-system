using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Invitations.Queries.GetInvitationsBySite;

public record GetInvitationsBySiteQuery : IRequest<InvitationsVm>
{
    public Guid SiteId { get; init; }
}

public class GetInvitationsBySiteQueryHandler : IRequestHandler<GetInvitationsBySiteQuery, InvitationsVm>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IIdentityService _identityService;

    public GetInvitationsBySiteQueryHandler(
        IApplicationDbContext context,
        IUser user,
        IIdentityService identityService)
    {
        _context = context;
        _user = user;
        _identityService = identityService;
    }

    public async Task<InvitationsVm> Handle(GetInvitationsBySiteQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _user.Id ?? throw new UnauthorizedAccessException("User not authenticated");

        // Site'ın var olduğunu ve kullanıcıya ait olduğunu kontrol et
        var site = await _context.Sites
            .FirstOrDefaultAsync(s => s.Id == request.SiteId && 
                                     !s.IsDeleted && 
                                     (s.UserId == currentUserId || 
                                      _context.UserSites.Any(us => us.SiteId == s.Id && us.UserId == currentUserId)), 
                                 cancellationToken);

        if (site == null)
        {
            throw new UnauthorizedAccessException("Bu site'a erişim yetkiniz yok.");
        }

        var invitations = await _context.Invitations
            .Where(i => i.SiteId == request.SiteId)
            .OrderByDescending(i => i.Created)
            .ToListAsync(cancellationToken);

        var invitationDtos = new List<InvitationDto>();

        foreach (var invitation in invitations)
        {
            var invitedByUserName = await _identityService.GetUserNameAsync(invitation.InvitedByUserId);

            invitationDtos.Add(new InvitationDto
            {
                Id = invitation.Id,
                Email = invitation.Email,
                InvitedUserId = invitation.InvitedUserId,
                InvitedByUserId = invitation.InvitedByUserId,
                InvitedByUserName = invitedByUserName ?? "Bilinmeyen",
                SiteId = invitation.SiteId,
                SiteName = site.Name,
                Token = invitation.Token,
                Status = invitation.Status,
                AcceptanceTime = invitation.AcceptanceTime,
                Created = invitation.Created
            });
        }

        return new InvitationsVm
        {
            WithUser = invitationDtos.Where(i => i.InvitedUserId != null).ToList(),
            WithoutUser = invitationDtos.Where(i => i.InvitedUserId == null).ToList()
        };
    }
}

public class InvitationsVm
{
    public List<InvitationDto> WithUser { get; init; } = new();
    public List<InvitationDto> WithoutUser { get; init; } = new();
}

