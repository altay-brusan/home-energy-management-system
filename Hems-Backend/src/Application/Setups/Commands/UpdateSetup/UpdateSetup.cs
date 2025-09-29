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

    public UpdateSetupCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdateSetupCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Setups
            .FindAsync([request.Id], cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        entity.SiteId = request.SiteId;
        entity.Name = request.Name;

        await _context.SaveChangesAsync(cancellationToken);
    }
}

















