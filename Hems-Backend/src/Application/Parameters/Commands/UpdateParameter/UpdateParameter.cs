using Hems_Backend.Application.Common.Exceptions;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using MediatR;

namespace Hems_Backend.Application.Parameters.Commands.UpdateParameter;

public record UpdateParameterCommand : IRequest
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string ElementType { get; init; } = string.Empty;
    public string RoleType { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public string DefaultValue { get; init; } = string.Empty;
    public int SortOrder { get; init; }
}

public class UpdateParameterCommandHandler : IRequestHandler<UpdateParameterCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly TimeProvider _timeProvider;

    public UpdateParameterCommandHandler(IApplicationDbContext context, IUser user, TimeProvider timeProvider)
    {
        _context = context;
        _user = user;
        _timeProvider = timeProvider;
    }

    public async Task Handle(UpdateParameterCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Parameters
            .FindAsync([request.Id], cancellationToken);

        if (entity == null)
        {
            throw AppException.NotFound(
                MessageCodes.PARAMETER_NOT_FOUND,
                new Dictionary<string, object> { ["parameterId"] = request.Id });
        }

        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.ElementType = request.ElementType;
        entity.RoleType = request.RoleType;
        entity.Unit = request.Unit;
        entity.DefaultValue = request.DefaultValue;
        entity.SortOrder = request.SortOrder;
        entity.LastModified = _timeProvider.GetUtcNow().DateTime;
        entity.LastModifiedBy = _user.Id;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
