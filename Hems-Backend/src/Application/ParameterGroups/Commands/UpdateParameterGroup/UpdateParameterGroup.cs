using Hems_Backend.Application.Common.Exceptions;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.ParameterGroups.Commands.UpdateParameterGroup;

public record UpdateParameterGroupCommand : IRequest
{
    public Guid Id { get; init; }
    public Guid? ParentId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Version { get; init; } = string.Empty;
    public int SortOrder { get; init; }
}

public class UpdateParameterGroupCommandHandler : IRequestHandler<UpdateParameterGroupCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly TimeProvider _timeProvider;

    public UpdateParameterGroupCommandHandler(IApplicationDbContext context, IUser user, TimeProvider timeProvider)
    {
        _context = context;
        _user = user;
        _timeProvider = timeProvider;
    }

    public async Task Handle(UpdateParameterGroupCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.ParameterGroups
            .FindAsync([request.Id], cancellationToken);

        if (entity == null)
        {
            throw AppException.NotFound(
                MessageCodes.PARAMETER_GROUP_NOT_FOUND,
                new Dictionary<string, object> { ["parameterGroupId"] = request.Id });
        }

        // Prevent circular reference
        if (request.ParentId.HasValue && request.ParentId.Value == request.Id)
        {
            throw AppException.BadRequest(
                MessageCodes.PARAMETER_GROUP_CIRCULAR_REF,
                new Dictionary<string, object> { ["parameterGroupId"] = request.Id });
        }

        // Validate ParentId
        if (request.ParentId.HasValue)
        {
            var parentExists = await _context.ParameterGroups
                .AnyAsync(pg => pg.Id == request.ParentId.Value, cancellationToken);

            if (!parentExists)
            {
                throw AppException.NotFound(
                    MessageCodes.PARAMETER_GROUP_NOT_FOUND,
                    new Dictionary<string, object> { ["parameterGroupId"] = request.ParentId.Value });
            }
        }

        entity.ParentId = request.ParentId;
        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.Version = request.Version;
        entity.SortOrder = request.SortOrder;
        entity.LastModified = _timeProvider.GetUtcNow().DateTime;
        entity.LastModifiedBy = _user.Id;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
