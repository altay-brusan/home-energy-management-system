using Hems_Backend.Application.Common.Exceptions;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Entities;
using Hems_Backend.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Invitations.Commands.DeclineInvitation;

public record DeclineInvitationCommand : IRequest<Unit>
{
    public string Token { get; init; } = string.Empty;
}

public class DeclineInvitationCommandHandler : IRequestHandler<DeclineInvitationCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IIdentityService _identityService;

    public DeclineInvitationCommandHandler(
        IApplicationDbContext context,
        IUser user,
        IIdentityService identityService)
    {
        _context = context;
        _user = user;
        _identityService = identityService;
    }

    public async Task<Unit> Handle(DeclineInvitationCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _user.Id ?? throw new UnauthorizedAccessException("User not authenticated");
        
        // Get current user's email
        var currentUserEmail = await _identityService.GetUserEmailAsync(currentUserId)
            ?? throw new UnauthorizedAccessException("User email address not found");

        // Find invitation by token
        var invitation = await _context.Invitations
            .FirstOrDefaultAsync(i => 
                i.Token == request.Token && 
                i.Status == InvitationStatus.Pending &&
                (i.Email == currentUserEmail || i.InvitedUserId == currentUserId),
                cancellationToken)
            ?? throw new Common.Exceptions.NotFoundException(nameof(Invitation), request.Token);

        // Update invitation status
        invitation.Status = InvitationStatus.Declined;

        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

