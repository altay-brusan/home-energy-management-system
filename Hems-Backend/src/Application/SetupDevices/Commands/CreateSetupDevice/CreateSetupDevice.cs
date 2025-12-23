using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.DeviceSettings.Commands.InitializeDeviceSettings;
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
    private readonly IMediator _mediator;

    public CreateSetupDeviceCommandHandler(
        IApplicationDbContext context,
        IUser user,
        TimeProvider timeProvider,
        IMediator mediator)
    {
        _context = context;
        _user = user;
        _timeProvider = timeProvider;
        _mediator = mediator;
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

        // Get device's model number to initialize settings
        var device = await _context.Devices
            .Where(d => d.Id == request.DeviceId)
            .Select(d => new { d.ModelNumber })
            .FirstOrDefaultAsync(cancellationToken);

        if (device != null && !string.IsNullOrEmpty(device.ModelNumber))
        {
            // Auto-populate device settings from scheme
            await _mediator.Send(new InitializeDeviceSettingsCommand
            {
                SetupDeviceId = entity.Id,
                ModelNumber = device.ModelNumber
            }, cancellationToken);
        }

        return entity.Id;
    }
}


