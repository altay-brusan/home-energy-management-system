using Hems_Backend.Application.Common.Exceptions;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.DeviceSettings.Commands.CreateSetupDeviceParameterValue;

public record CreateSetupDeviceParameterValueCommand : IRequest<Guid>
{
    public Guid? ParameterGroupId { get; init; }
    public Guid ParameterId { get; init; }
    public Guid SetupDeviceId { get; init; }
    public string Value { get; init; } = string.Empty;
    public int SortOrder { get; init; }
    public string ModelNumber { get; init; } = string.Empty;
}

public class CreateSetupDeviceParameterValueCommandHandler : IRequestHandler<CreateSetupDeviceParameterValueCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly TimeProvider _timeProvider;

    public CreateSetupDeviceParameterValueCommandHandler(IApplicationDbContext context, IUser user, TimeProvider timeProvider)
    {
        _context = context;
        _user = user;
        _timeProvider = timeProvider;
    }

    public async Task<Guid> Handle(CreateSetupDeviceParameterValueCommand request, CancellationToken cancellationToken)
    {
        // Check unique constraint
        var existingRecord = await _context.SetupDeviceParameterValues
            .AnyAsync(sdpv => sdpv.SetupDeviceId == request.SetupDeviceId && sdpv.ParameterId == request.ParameterId, cancellationToken);

        if (existingRecord)
        {
            throw AppException.BadRequest(
                MessageCodes.PARAMETER_ALREADY_EXISTS,
                new Dictionary<string, object> { ["setupDeviceId"] = request.SetupDeviceId, ["parameterId"] = request.ParameterId });
        }

        // Validate ParameterId
        var parameterExists = await _context.Parameters
            .AnyAsync(p => p.Id == request.ParameterId, cancellationToken);

        if (!parameterExists)
        {
            throw AppException.NotFound(
                MessageCodes.PARAMETER_NOT_FOUND,
                new Dictionary<string, object> { ["parameterId"] = request.ParameterId });
        }

        // Validate SetupDeviceId
        var setupDeviceExists = await _context.SetupDevices
            .AnyAsync(sd => sd.Id == request.SetupDeviceId, cancellationToken);

        if (!setupDeviceExists)
        {
            throw AppException.NotFound(
                MessageCodes.DEVICE_NOT_FOUND,
                new Dictionary<string, object> { ["setupDeviceId"] = request.SetupDeviceId });
        }

        // Validate ParameterGroupId if provided
        if (request.ParameterGroupId.HasValue)
        {
            var groupExists = await _context.ParameterGroups
                .AnyAsync(pg => pg.Id == request.ParameterGroupId.Value, cancellationToken);

            if (!groupExists)
            {
                throw AppException.NotFound(
                    MessageCodes.PARAMETER_GROUP_NOT_FOUND,
                    new Dictionary<string, object> { ["parameterGroupId"] = request.ParameterGroupId.Value });
            }
        }

        var entity = new SetupDeviceParameterValue
        {
            ParameterGroupId = request.ParameterGroupId,
            ParameterId = request.ParameterId,
            SetupDeviceId = request.SetupDeviceId,
            Value = request.Value,
            SortOrder = request.SortOrder,
            ModelNumber = request.ModelNumber,
            Created = _timeProvider.GetUtcNow().DateTime,
            CreatedBy = _user.Id
        };

        _context.SetupDeviceParameterValues.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
