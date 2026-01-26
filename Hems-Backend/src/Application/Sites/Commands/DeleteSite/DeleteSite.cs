using Hems_Backend.Application.Common.Exceptions;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
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
        var userId = _user.Id
            ?? throw AppException.Unauthorized(MessageCodes.AUTH_USER_NOT_AUTHENTICATED);

        // Verify site exists
        var entity = await _context.Sites
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken)
            ?? throw AppException.NotFound(
                MessageCodes.SITE_NOT_FOUND,
                new Dictionary<string, object> { ["siteId"] = request.Id });

        // Only the site owner can delete the site
        if (entity.UserId != userId)
        {
            throw AppException.Forbidden(
                MessageCodes.SITE_ONLY_OWNER_CAN_DELETE,
                new Dictionary<string, object> { ["siteId"] = request.Id });
        }

        // Check if site has a setup
        var hasSetup = await _context.Setups
            .AnyAsync(s => s.SiteId == request.Id, cancellationToken);

        if (hasSetup)
        {
            throw AppException.BadRequest(
                MessageCodes.SITE_CANNOT_DELETE_WITH_SETUP,
                new Dictionary<string, object> { ["siteId"] = request.Id });
        }

        // Soft delete
        entity.IsDeleted = true;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
