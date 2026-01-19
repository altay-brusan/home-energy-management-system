using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.MobileAppVersions.Commands.UpdateMobileAppVersion;

public record UpdateMobileAppVersionCommand : IRequest
{
    public Guid Id { get; init; }
    public string? StoreUrl { get; init; }
    public string? ReleaseNotes { get; init; }
    public bool IsMandatory { get; init; }
    public string? MinimumRequiredVersion { get; init; }
    public DateTimeOffset? ReleasedAt { get; init; }
}

public class UpdateMobileAppVersionCommandHandler : IRequestHandler<UpdateMobileAppVersionCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateMobileAppVersionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdateMobileAppVersionCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.MobileAppVersions
            .FirstOrDefaultAsync(v => v.Id == request.Id, cancellationToken);

        if (entity == null)
        {
            throw new Common.Exceptions.NotFoundException(nameof(MobileAppVersion), request.Id);
        }

        entity.StoreUrl = request.StoreUrl;
        entity.ReleaseNotes = request.ReleaseNotes;
        entity.IsMandatory = request.IsMandatory;
        entity.MinimumRequiredVersion = request.MinimumRequiredVersion;
        entity.ReleasedAt = request.ReleasedAt;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
