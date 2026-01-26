using Hems_Backend.Application.Common.Exceptions;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.ParameterValueSettingSchemes.Commands.CreateParameterValueSettingScheme;

public record CreateParameterValueSettingSchemeCommand : IRequest<Guid>
{
    public Guid ParameterId { get; init; }
    public Guid? ParameterGroupId { get; init; }
    public string Value { get; init; } = string.Empty;
    public int SortOrder { get; init; }
    public string ModelNumber { get; init; } = string.Empty;
}

public class CreateParameterValueSettingSchemeCommandHandler : IRequestHandler<CreateParameterValueSettingSchemeCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly TimeProvider _timeProvider;

    public CreateParameterValueSettingSchemeCommandHandler(IApplicationDbContext context, IUser user, TimeProvider timeProvider)
    {
        _context = context;
        _user = user;
        _timeProvider = timeProvider;
    }

    public async Task<Guid> Handle(CreateParameterValueSettingSchemeCommand request, CancellationToken cancellationToken)
    {
        // Validate ParameterId
        var parameterExists = await _context.Parameters
            .AnyAsync(p => p.Id == request.ParameterId, cancellationToken);

        if (!parameterExists)
        {
            throw AppException.NotFound(
                MessageCodes.PARAMETER_NOT_FOUND,
                new Dictionary<string, object> { ["parameterId"] = request.ParameterId });
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

        var entity = new ParameterValueSettingScheme
        {
            ParameterId = request.ParameterId,
            ParameterGroupId = request.ParameterGroupId,
            Value = request.Value,
            SortOrder = request.SortOrder,
            ModelNumber = request.ModelNumber,
            Created = _timeProvider.GetUtcNow().DateTime,
            CreatedBy = _user.Id
        };

        _context.ParameterValueSettingSchemes.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
