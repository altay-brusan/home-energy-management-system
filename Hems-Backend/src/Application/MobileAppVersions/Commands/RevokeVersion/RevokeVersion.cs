using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.MobileAppVersions.Commands.RevokeVersion;

public record RevokeVersionCommand : IRequest
{
    public Guid Id { get; init; }
    public string? Reason { get; init; }
}

public class RevokeVersionCommandHandler : IRequestHandler<RevokeVersionCommand>
{
    private readonly IApplicationDbContext _context;

    public RevokeVersionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(RevokeVersionCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.MobileAppVersions
            .FirstOrDefaultAsync(v => v.Id == request.Id, cancellationToken);

        if (entity == null)
        {
            throw new Common.Exceptions.NotFoundException(nameof(MobileAppVersion), request.Id);
        }

        entity.IsRevoked = true;
        entity.RevocationReason = request.Reason;

        // If the revoked version was marked as latest, unmark it
        if (entity.IsLatest)
        {
            entity.IsLatest = false;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
