using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.MobileAppVersions.Commands.DeleteMobileAppVersion;

public record DeleteMobileAppVersionCommand(Guid Id) : IRequest;

public class DeleteMobileAppVersionCommandHandler : IRequestHandler<DeleteMobileAppVersionCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteMobileAppVersionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteMobileAppVersionCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.MobileAppVersions
            .FirstOrDefaultAsync(v => v.Id == request.Id, cancellationToken);

        if (entity == null)
        {
            throw new Common.Exceptions.NotFoundException(nameof(MobileAppVersion), request.Id);
        }

        // Soft delete by setting IsActive to false
        entity.IsActive = false;

        // If this was the latest version, we need to unmark it
        if (entity.IsLatest)
        {
            entity.IsLatest = false;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
