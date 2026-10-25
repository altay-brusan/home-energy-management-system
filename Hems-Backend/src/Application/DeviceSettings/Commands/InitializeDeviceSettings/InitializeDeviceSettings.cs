using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Entities;
using MediatR;

namespace Hems_Backend.Application.DeviceSettings.Commands.InitializeDeviceSettings;

public record InitializeDeviceSettingsCommand : IRequest<int>
{
    public Guid SetupDeviceId { get; init; }
    public string ModelNumber { get; init; } = string.Empty;
}

public class InitializeDeviceSettingsCommandHandler : IRequestHandler<InitializeDeviceSettingsCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly TimeProvider _timeProvider;

    public InitializeDeviceSettingsCommandHandler(
        IApplicationDbContext context,
        IUser user,
        TimeProvider timeProvider)
    {
        _context = context;
        _user = user;
        _timeProvider = timeProvider;
    }

    public async Task<int> Handle(InitializeDeviceSettingsCommand request, CancellationToken cancellationToken)
    {
        // Get all parameter value setting schemes for this model
        var schemes = await _context.ParameterValueSettingSchemes
            .Where(pvss => pvss.ModelNumber == request.ModelNumber)
            .ToListAsync(cancellationToken);

        if (schemes.Count == 0)
        {
            // No schemes found for this model, return 0
            return 0;
        }

        // Check if settings already exist for this device
        var existingSettings = await _context.SetupDeviceParameterValues
            .Where(sdpv => sdpv.SetupDeviceId == request.SetupDeviceId)
            .ToListAsync(cancellationToken);

        if (existingSettings.Any())
        {
            // Settings already exist, don't overwrite
            return 0;
        }

        var utcNow = _timeProvider.GetUtcNow();

        // Create SetupDeviceParameterValues from schemes
        var deviceParameterValues = schemes.Select(scheme => new SetupDeviceParameterValue
        {
            SetupDeviceId = request.SetupDeviceId,
            ParameterId = scheme.ParameterId,
            ParameterGroupId = scheme.ParameterGroupId,
            Value = scheme.Value,
            SortOrder = scheme.SortOrder,
            ModelNumber = scheme.ModelNumber,
            Created = utcNow,
            CreatedBy = _user.Id,
            LastModified = utcNow,
            LastModifiedBy = _user.Id
        }).ToList();

        _context.SetupDeviceParameterValues.AddRange(deviceParameterValues);
        await _context.SaveChangesAsync(cancellationToken);

        return deviceParameterValues.Count;
    }
}
