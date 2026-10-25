using Hems_Backend.Application.Common.Exceptions;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using MediatR;

namespace Hems_Backend.Application.ParameterValueSettingSchemes.Commands.UpdateParameterValueSettingScheme;

public record UpdateParameterValueSettingSchemeCommand : IRequest
{
    public Guid Id { get; init; }
    public string Value { get; init; } = string.Empty;
    public int SortOrder { get; init; }
    public string ModelNumber { get; init; } = string.Empty;
}

public class UpdateParameterValueSettingSchemeCommandHandler : IRequestHandler<UpdateParameterValueSettingSchemeCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly TimeProvider _timeProvider;

    public UpdateParameterValueSettingSchemeCommandHandler(IApplicationDbContext context, IUser user, TimeProvider timeProvider)
    {
        _context = context;
        _user = user;
        _timeProvider = timeProvider;
    }

    public async Task Handle(UpdateParameterValueSettingSchemeCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.ParameterValueSettingSchemes
            .FindAsync([request.Id], cancellationToken);

        if (entity == null)
        {
            throw AppException.NotFound(
                MessageCodes.RECORD_NOT_FOUND,
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
