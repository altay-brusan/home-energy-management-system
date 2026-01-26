using Hems_Backend.Application.Common.Exceptions;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using MediatR;

namespace Hems_Backend.Application.DeviceSettings.Commands.UpdateSetupDeviceParameterValue;

public record UpdateSetupDeviceParameterValueCommand : IRequest
{
    public Guid Id { get; init; }
    public string Value { get; init; } = string.Empty;
    public int SortOrder { get; init; }
    public string ModelNumber { get; init; } = string.Empty;
}

public class UpdateSetupDeviceParameterValueCommandHandler : IRequestHandler<UpdateSetupDeviceParameterValueCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly TimeProvider _timeProvider;

    public UpdateSetupDeviceParameterValueCommandHandler(IApplicationDbContext context, IUser user, TimeProvider timeProvider)
    {
        _context = context;
        _user = user;
        _timeProvider = timeProvider;
    }

    public async Task Handle(UpdateSetupDeviceParameterValueCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.SetupDeviceParameterValues
            .FindAsync([request.Id], cancellationToken);

        if (entity == null)
        {
            throw AppException.NotFound(
                MessageCodes.PARAMETER_VALUE_NOT_FOUND,
                new Dictionary<string, object> { ["id"] = request.Id });
        }

        entity.Value = request.Value;
        entity.SortOrder = request.SortOrder;
        entity.ModelNumber = request.ModelNumber;
        entity.LastModified = _timeProvider.GetUtcNow().DateTime;
        entity.LastModifiedBy = _user.Id;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
