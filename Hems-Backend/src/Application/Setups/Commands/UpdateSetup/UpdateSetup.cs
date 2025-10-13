using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Setups.Commands.UpdateSetup;

public record UpdateSetupCommand : IRequest
{
    public Guid Id { get; init; }
    public Guid SiteId { get; init; }
    public string Name { get; init; } = string.Empty;
}

public class UpdateSetupCommandHandler : IRequestHandler<UpdateSetupCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public UpdateSetupCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(UpdateSetupCommand request, CancellationToken cancellationToken)
    {
        var userId = _user.Id ?? throw new UnauthorizedAccessException("User not authenticated");

        var entity = await _context.Setups
            .Include(s => s.Site)
            .FirstOrDefaultAsync(s => s.Id == request.Id && 
                                     (s.Site.UserId == userId || 
                                      _context.UserSites.Any(us => us.SiteId == s.SiteId && us.UserId == userId)), 
                                 cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        entity.SiteId = request.SiteId;
        entity.Name = request.Name;

        await _context.SaveChangesAsync(cancellationToken);
    }
}




















