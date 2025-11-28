using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Entities;
using MediatR;

namespace Hems_Backend.Application.Devices.Commands.DeleteDevice;

public record DeleteDeviceCommand(Guid Id) : IRequest;

public class DeleteDeviceCommandHandler : IRequestHandler<DeleteDeviceCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IThingsBoardConnectorService _tbConnector;

    public DeleteDeviceCommandHandler(
        IApplicationDbContext context,
        IThingsBoardConnectorService tbConnector)
    {
        _context = context;
        _tbConnector = tbConnector;
    }

    public async Task Handle(DeleteDeviceCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Devices
            .FindAsync([request.Id], cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        // Delete from ThingsBoard first
        await _tbConnector.DeleteDeviceAsync(entity.ThingsBoardDeviceId, cancellationToken);

        // Then delete from local database
        _context.Devices.Remove(entity);

        await _context.SaveChangesAsync(cancellationToken);
    }
}



















