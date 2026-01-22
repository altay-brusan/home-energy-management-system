using Hems_Backend.Application.Common.Exceptions;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.DeviceSettings.Commands.UpdateDeviceParameterValue;

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
            throw AppException.NotFound(
                MessageCodes.DEVICE_NOT_IN_SETUP,
                new Dictionary<string, object> { ["deviceId"] = request.DeviceId });
        }

        // Find the parameter value for this device and parameter
        var entity = await _context.SetupDeviceParameterValues
            .Where(sdpv => sdpv.SetupDeviceId == setupDevice.Id && sdpv.ParameterId == request.ParameterId)
            .FirstOrDefaultAsync(cancellationToken);

        if (entity == null)
        {
            throw AppException.NotFound(
                MessageCodes.PARAMETER_VALUE_NOT_FOUND,
                new Dictionary<string, object> { ["deviceId"] = request.DeviceId, ["parameterId"] = request.ParameterId });
        }

        entity.Value = request.Value;
        entity.LastModified = _timeProvider.GetUtcNow().DateTime;
        entity.LastModifiedBy = _user.Id;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
