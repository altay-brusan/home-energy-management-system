using Hems_Backend.Application.Common.Exceptions;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.MobileAppVersions.Commands.SetLatestVersion;

public record SetLatestVersionCommand(Guid Id) : IRequest;

public class SetLatestVersionCommandHandler : IRequestHandler<SetLatestVersionCommand>
{
    private readonly IApplicationDbContext _context;

    public SetLatestVersionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(SetLatestVersionCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.MobileAppVersions
            .FirstOrDefaultAsync(v => v.Id == request.Id, cancellationToken);

        if (entity == null)
        {
            throw AppException.NotFound(
                MessageCodes.VERSION_NOT_FOUND,
                new Dictionary<string, object> { ["versionId"] = request.Id });
        }

        if (!entity.IsActive)
        {
            throw AppException.BadRequest(
                MessageCodes.VERSION_CANNOT_SET_INACTIVE_LATEST,
                new Dictionary<string, object> { ["versionId"] = request.Id });
        }

        // Unmark current latest version for this platform
        var currentLatest = await _context.MobileAppVersions
            .Where(v => v.Platform == entity.Platform && v.IsLatest && v.Id != entity.Id)
            .ToListAsync(cancellationToken);

        foreach (var v in currentLatest)
        {
            v.IsLatest = false;
        }

        // Mark the requested version as latest
        entity.IsLatest = true;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
