using Hems_Backend.Application.Common.Exceptions;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.ParameterGroups.Commands.CreateParameterGroup;

public record CreateParameterGroupCommand : IRequest<Guid>
{
    public Guid? ParentId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Version { get; init; } = string.Empty;
    public int SortOrder { get; init; }
}

public class CreateParameterGroupCommandHandler : IRequestHandler<CreateParameterGroupCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly TimeProvider _timeProvider;

    public CreateParameterGroupCommandHandler(IApplicationDbContext context, IUser user, TimeProvider timeProvider)
    {
        _context = context;
        _user = user;
        _timeProvider = timeProvider;
    }

    public async Task<Guid> Handle(CreateParameterGroupCommand request, CancellationToken cancellationToken)
    {
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

        var entity = new ParameterGroup
        {
            ParentId = request.ParentId,
            Name = request.Name,
            Description = request.Description,
            Version = request.Version,
            SortOrder = request.SortOrder,
            Created = _timeProvider.GetUtcNow().DateTime,
            CreatedBy = _user.Id
        };

        _context.ParameterGroups.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
