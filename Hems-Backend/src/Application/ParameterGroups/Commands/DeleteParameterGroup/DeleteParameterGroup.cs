using Hems_Backend.Application.Common.Exceptions;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.ParameterGroups.Commands.DeleteParameterGroup;

public record DeleteParameterGroupCommand : IRequest
{
    public Guid Id { get; init; }
}

public class DeleteParameterGroupCommandHandler : IRequestHandler<DeleteParameterGroupCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteParameterGroupCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteParameterGroupCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.ParameterGroups
            .FindAsync([request.Id], cancellationToken);

        if (entity == null)
        {
            throw AppException.NotFound(
                MessageCodes.PARAMETER_GROUP_NOT_FOUND,
                new Dictionary<string, object> { ["parameterGroupId"] = request.Id });
        }

        // Check for subgroups
        var hasChildren = await _context.ParameterGroups
            .AnyAsync(pg => pg.ParentId == request.Id, cancellationToken);

        if (hasChildren)
        {
            throw AppException.BadRequest(
                MessageCodes.PARAMETER_GROUP_HAS_SUBGROUPS,
                new Dictionary<string, object> { ["parameterGroupId"] = request.Id });
        }

        // Check for parameters in this group
        var hasParameters = await _context.Parameters
            .AnyAsync(p => p.ParameterGroupId == request.Id, cancellationToken);

        if (hasParameters)
        {
            throw AppException.BadRequest(
                MessageCodes.PARAMETER_GROUP_HAS_PARAMETERS,
                new Dictionary<string, object> { ["parameterGroupId"] = request.Id });
        }

        _context.ParameterGroups.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
