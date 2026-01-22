using Hems_Backend.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Sites.Commands.DeleteSite;

public record DeleteSiteCommand(Guid Id) : IRequest;

public class DeleteSiteCommandHandler : IRequestHandler<DeleteSiteCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public DeleteSiteCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(DeleteSiteCommand request, CancellationToken cancellationToken)
    {
        var userId = _user.Id ?? throw new UnauthorizedAccessException("User not authenticated");

        // Verify site exists
        var entity = await _context.Sites
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken)
            ?? throw new Hems_Backend.Application.Common.Exceptions.NotFoundException(nameof(Domain.Entities.Site), request.Id);

        // Only the site owner can delete the site
        if (entity.UserId != userId)
        {
            throw new Hems_Backend.Application.Common.Exceptions.ForbiddenAccessException(
                "Only the site owner can delete this site. You do not have permission to delete it.");
        }

        // Site'ın setup'ı var mı kontrol et
        var hasSetup = await _context.Setups
            .AnyAsync(s => s.SiteId == request.Id, cancellationToken);

        if (hasSetup)
        {
            throw new InvalidOperationException("Bu site'ın bir setup'ı var. Önce setup'ı silmeniz gerekiyor.");
        }

        // Soft delete
        entity.IsDeleted = true;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
