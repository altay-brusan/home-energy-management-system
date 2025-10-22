using Hems_Backend.Application.Common.Interfaces;
using MediatR;

namespace Hems_Backend.Application.SetupDeviceParameterValues.Commands.UpdateSetupDeviceParameterValue;

public record UpdateSetupDeviceParameterValueCommand : IRequest
{
    public Guid Id { get; init; }
    public Guid? ParameterGroupId { get; init; }
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
            throw new ArgumentException("Kayıt bulunamadı.");
        }

        // ParameterGroupId doğrulama (varsa)
        if (request.ParameterGroupId.HasValue)
        {
            var groupExists = await _context.ParameterGroups
                .AnyAsync(pg => pg.Id == request.ParameterGroupId.Value, cancellationToken);
            
            if (!groupExists)
            {
                throw new ArgumentException("Parametre grubu bulunamadı.");
            }
        }

        entity.ParameterGroupId = request.ParameterGroupId;
        entity.Value = request.Value;
        entity.SortOrder = request.SortOrder;
        entity.ModelNumber = request.ModelNumber;
        entity.LastModified = _timeProvider.GetUtcNow().DateTime;
        entity.LastModifiedBy = _user.Id;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
