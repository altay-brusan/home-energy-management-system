using Hems_Backend.Application.Common.Interfaces;
using MediatR;

namespace Hems_Backend.Application.SetupDeviceParameterValues.Commands.UpdateDeviceParameterValue;

public record UpdateDeviceParameterValueCommand : IRequest
{
    public Guid DeviceId { get; init; }
    public Guid ParameterId { get; init; }
    public string Value { get; init; } = string.Empty;
}

public class UpdateDeviceParameterValueCommandHandler : IRequestHandler<UpdateDeviceParameterValueCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly TimeProvider _timeProvider;

    public UpdateDeviceParameterValueCommandHandler(
        IApplicationDbContext context,
        IUser user,
        TimeProvider timeProvider)
    {
        _context = context;
        _user = user;
        _timeProvider = timeProvider;
    }

    public async Task Handle(UpdateDeviceParameterValueCommand request, CancellationToken cancellationToken)
    {
        // Find the SetupDevice for this Device
        var setupDevice = await _context.SetupDevices
            .Where(sd => sd.DeviceId == request.DeviceId)
            .FirstOrDefaultAsync(cancellationToken);

        if (setupDevice == null)
        {
            throw new ArgumentException("Device not found in any setup.");
        }

        // Find the parameter value for this device and parameter
        var entity = await _context.SetupDeviceParameterValues
            .Where(sdpv => sdpv.SetupDeviceId == setupDevice.Id && sdpv.ParameterId == request.ParameterId)
            .FirstOrDefaultAsync(cancellationToken);

        if (entity == null)
        {
            throw new ArgumentException("Parameter value not found for this device.");
        }

        entity.Value = request.Value;
        entity.LastModified = _timeProvider.GetUtcNow().DateTime;
        entity.LastModifiedBy = _user.Id;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
