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
        
        // Kullanıcının email'ini al
        var currentUserEmail = await _identityService.GetUserEmailAsync(currentUserId)
            ?? throw new UnauthorizedAccessException("Kullanıcı email adresi bulunamadı");

        // Token ile davetiyeyi bul
        var invitation = await _context.Invitations
            .FirstOrDefaultAsync(i => 
                i.Token == request.Token && 
                i.Status == InvitationStatus.Pending &&
                (i.Email == currentUserEmail || i.InvitedUserId == currentUserId),
                cancellationToken)
            ?? throw new Common.Exceptions.NotFoundException(nameof(Invitation), request.Token);

        var utcNow = _timeProvider.GetUtcNow();

        // Invitation'ı güncelle
        invitation.Status = InvitationStatus.Accepted;
        invitation.AcceptanceTime = utcNow;
        
        // Eğer InvitedUserId boşsa, şimdi güncelle
        if (string.IsNullOrEmpty(invitation.InvitedUserId))
        {
            invitation.InvitedUserId = currentUserId;
        }

        // UserSite kaydı oluştur (önce kontrol et, zaten varsa hata ver)
        var existingUserSite = await _context.UserSites
            .FirstOrDefaultAsync(us => us.UserId == currentUserId && us.SiteId == invitation.SiteId, cancellationToken);

        if (existingUserSite != null)
        {
            throw new InvalidOperationException("Bu site zaten sizinle paylaşılmış.");
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

        // Notification'ı güncelle
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

