using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.DeviceComponents.Commands.DeleteDeviceComponent;

public record DeleteDeviceComponentCommand : IRequest
{
    public Guid Id { get; init; }
}

public class DeleteDeviceComponentCommandHandler : IRequestHandler<DeleteDeviceComponentCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteDeviceComponentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteDeviceComponentCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.DeviceComponents
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (entity == null)
        {
            throw new Hems_Backend.Application.Common.Exceptions.NotFoundException(nameof(DeviceComponent), request.Id);
        }

        // Soft delete
        entity.IsDeleted = true;

        await _context.SaveChangesAsync(cancellationToken);
    }
}

