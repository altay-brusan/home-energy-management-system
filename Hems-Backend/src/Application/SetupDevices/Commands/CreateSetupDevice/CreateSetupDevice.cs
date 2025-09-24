using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Entities;
using MediatR;

namespace Hems_Backend.Application.SetupDevices.Commands.CreateSetupDevice;

public record CreateSetupDeviceCommand : IRequest<Guid>
{
    public Guid SetupId { get; init; }
    public Guid DeviceId { get; init; }
}

public class CreateSetupDeviceCommandHandler : IRequestHandler<CreateSetupDeviceCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly TimeProvider _timeProvider;

    public CreateSetupDeviceCommandHandler(IApplicationDbContext context, IUser user, TimeProvider timeProvider)
    {
        _context = context;
        _user = user;
        _timeProvider = timeProvider;
    }

    public async Task<Guid> Handle(CreateSetupDeviceCommand request, CancellationToken cancellationToken)
    {
        var entity = new SetupDevice
        {
            SetupId = request.SetupId,
            DeviceId = request.DeviceId,
        };

        _context.SetupDevices.Add(entity);

        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}


