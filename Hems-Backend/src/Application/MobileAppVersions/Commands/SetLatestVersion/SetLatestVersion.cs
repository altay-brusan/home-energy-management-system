using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Entities;
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
            throw new Common.Exceptions.NotFoundException(nameof(MobileAppVersion), request.Id);
        }

        if (!entity.IsActive)
        {
            throw new InvalidOperationException("Cannot set an inactive version as latest.");
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
