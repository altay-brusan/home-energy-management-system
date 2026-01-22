using Hems_Backend.Application.Common.Exceptions;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using MediatR;

namespace Hems_Backend.Application.DeviceSettings.Commands.DeleteSetupDeviceParameterValue;

public record DeleteSetupDeviceParameterValueCommand : IRequest
{
    public Guid Id { get; init; }
}

public class DeleteSetupDeviceParameterValueCommandHandler : IRequestHandler<DeleteSetupDeviceParameterValueCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteSetupDeviceParameterValueCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteSetupDeviceParameterValueCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.SetupDeviceParameterValues
            .FindAsync([request.Id], cancellationToken);

        if (entity == null)
        {
            throw AppException.NotFound(
                MessageCodes.PARAMETER_VALUE_NOT_FOUND,
                new Dictionary<string, object> { ["id"] = request.Id });
        }

        _context.SetupDeviceParameterValues.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
