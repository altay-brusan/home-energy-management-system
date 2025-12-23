using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Entities;
using MediatR;

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
        // Unique constraint kontrolü
        var existingRecord = await _context.SetupDeviceParameterValues
            .AnyAsync(sdpv => sdpv.SetupDeviceId == request.SetupDeviceId && sdpv.ParameterId == request.ParameterId, cancellationToken);

        if (existingRecord)
        {
            throw new InvalidOperationException("Bu setup device için bu parametre zaten mevcut.");
        }

        // ParameterId doğrulama
        var parameterExists = await _context.Parameters
            .AnyAsync(p => p.Id == request.ParameterId, cancellationToken);

        if (!parameterExists)
        {
            throw new ArgumentException("Parametre bulunamadı.");
        }

        // SetupDeviceId doğrulama
        var setupDeviceExists = await _context.SetupDevices
            .AnyAsync(sd => sd.Id == request.SetupDeviceId, cancellationToken);

        if (!setupDeviceExists)
        {
            throw new ArgumentException("Setup device bulunamadı.");
        }

        // ParameterGroupId doğrulama (varsa)
        if (request.ParameterGroupId.HasValue)
        {
            var groupExists = await _context.ParameterGroups
                .AnyAsync(pg => pg.Id == request.ParameterGroupId.Value, cancellationToken);

            if (!groupExists)
            {
                throw new ArgumentException("Parametre grubu bulunamadı.");
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
