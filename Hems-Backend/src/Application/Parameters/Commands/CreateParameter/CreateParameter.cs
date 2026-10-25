using Hems_Backend.Application.Common.Exceptions;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Parameters.Commands.CreateParameter;

public record CreateParameterCommand : IRequest<Guid>
{
    public Guid ParameterGroupId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string ElementType { get; init; } = string.Empty;
    public string RoleType { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public string DefaultValue { get; init; } = string.Empty;
    public int SortOrder { get; init; }
}

public class CreateParameterCommandHandler : IRequestHandler<CreateParameterCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly TimeProvider _timeProvider;

    public CreateParameterCommandHandler(IApplicationDbContext context, IUser user, TimeProvider timeProvider)
    {
        _context = context;
        _user = user;
        _timeProvider = timeProvider;
    }

    public async Task<Guid> Handle(CreateParameterCommand request, CancellationToken cancellationToken)
    {
        // Validate ParameterGroupId
        var groupExists = await _context.ParameterGroups
            .AnyAsync(pg => pg.Id == request.ParameterGroupId, cancellationToken);

        if (!groupExists)
        {
            throw AppException.NotFound(
                MessageCodes.PARAMETER_GROUP_NOT_FOUND,
                new Dictionary<string, object> { ["parameterGroupId"] = request.ParameterGroupId });
        }

        var entity = new Parameter
        {
            ParameterGroupId = request.ParameterGroupId,
            Name = request.Name,
            Description = request.Description,
            ElementType = request.ElementType,
            RoleType = request.RoleType,
            Unit = request.Unit,
            DefaultValue = request.DefaultValue,
            SortOrder = request.SortOrder,
            Created = _timeProvider.GetUtcNow().DateTime,
            CreatedBy = _user.Id
        };

        _context.Parameters.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
