using Hems_Backend.Application.Common.Exceptions;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Parameters.Commands.DeleteParameter;

public record DeleteParameterCommand : IRequest
{
    public Guid Id { get; init; }
}

public class DeleteParameterCommandHandler : IRequestHandler<DeleteParameterCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteParameterCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteParameterCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Parameters
            .FindAsync([request.Id], cancellationToken);

        if (entity == null)
        {
            throw AppException.NotFound(
                MessageCodes.PARAMETER_NOT_FOUND,
                new Dictionary<string, object> { ["parameterId"] = request.Id });
        }

        // Check for references in SetupDeviceParameterValues
        var hasSetupDeviceParameterValues = await _context.SetupDeviceParameterValues
            .AnyAsync(sdpv => sdpv.ParameterId == request.Id, cancellationToken);

        if (hasSetupDeviceParameterValues)
        {
            throw AppException.BadRequest(
                MessageCodes.PARAMETER_IN_USE,
                new Dictionary<string, object> { ["parameterId"] = request.Id });
        }

        // Check for references in ParameterValueSettingScheme
        var hasParameterValueSettingSchemes = await _context.ParameterValueSettingSchemes
            .AnyAsync(pvss => pvss.ParameterId == request.Id, cancellationToken);

        if (hasParameterValueSettingSchemes)
        {
            throw AppException.BadRequest(
                MessageCodes.PARAMETER_IN_SCHEMA,
                new Dictionary<string, object> { ["parameterId"] = request.Id });
        }

        _context.Parameters.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
