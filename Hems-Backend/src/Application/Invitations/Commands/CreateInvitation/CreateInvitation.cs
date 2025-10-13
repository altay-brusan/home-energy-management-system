using Hems_Backend.Application.Common.Exceptions;
using Hems_Backend.Application.Common.Interfaces;
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
        var currentUserId = _user.Id ?? throw new UnauthorizedAccessException("User not authenticated");

        // Site'ın var olduğunu ve kullanıcıya ait olduğunu kontrol et
        var site = await _context.Sites
            .FirstOrDefaultAsync(s => s.Id == request.SiteId && 
                                     !s.IsDeleted && 
                                     (s.UserId == currentUserId || 
                                      _context.UserSites.Any(us => us.SiteId == s.Id && us.UserId == currentUserId)), 
                                 cancellationToken)
            ?? throw new Common.Exceptions.NotFoundException(nameof(Site), request.SiteId);

        // Email ile kullanıcı ara
        var invitedUserId = await _identityService.GetUserIdByEmailAsync(request.Email);

        // Daha önce pending davetiye olup olmadığını kontrol et
        var existingInvitation = await _context.Invitations
            .FirstOrDefaultAsync(i => 
                i.Email == request.Email && 
                i.SiteId == request.SiteId && 
                i.Status == InvitationStatus.Pending, 
                cancellationToken);

        if (existingInvitation != null)
        {
            throw new InvalidOperationException("Bu kullanıcıya bu ev için zaten bekleyen bir davetiye var.");
        }

        // Token oluştur (6 haneli rastgele sayı)
        var token = await GenerateUniqueTokenAsync(cancellationToken);

        var utcNow = _timeProvider.GetUtcNow();

        // Invitation kaydı oluştur
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

        // Notification kaydı oluştur
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            UserId = invitedUserId,
            InvitationId = invitation.Id,
            Message = $"Kullanıcı size {site.Name} evini paylaştı.",
            IsRead = false,
            Created = utcNow
        };

        _context.Notifications.Add(notification);

        await _context.SaveChangesAsync(cancellationToken);

        // Email gönder
        var currentUserName = await _identityService.GetUserNameAsync(currentUserId);
        await _emailService.SendInvitationEmailAsync(
            request.Email,
            currentUserName ?? "Kullanıcı",
            site.Name,
            token,
            cancellationToken);

        return invitation.Id;
    }

    private async Task<string> GenerateUniqueTokenAsync(CancellationToken cancellationToken)
    {
        var random = new Random();
        var tenMinutesAgo = _timeProvider.GetUtcNow().AddMinutes(-10);
        
        string token;
        bool isUnique;
        
        do
        {
            // 6 haneli rastgele sayı
            token = random.Next(100000, 999999).ToString();
            
            // Son 10 dakikada bu token kullanılmış mı kontrol et
            isUnique = !await _context.Invitations
                .AnyAsync(i => i.Token == token && i.Created > tenMinutesAgo, cancellationToken);
                
        } while (!isUnique);

        return token;
    }
}

