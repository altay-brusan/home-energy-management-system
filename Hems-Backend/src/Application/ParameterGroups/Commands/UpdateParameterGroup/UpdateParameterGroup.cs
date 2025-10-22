using Hems_Backend.Application.Common.Interfaces;
using MediatR;

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
            throw new ArgumentException("Kayıt bulunamadı.");
        }

        // ParentId değişikliği kontrolü - sonsuz döngü engelleme
        if (request.ParentId.HasValue && request.ParentId.Value == request.Id)
        {
            throw new ArgumentException("Bir grup kendisinin alt grubu olamaz.");
        }

        // ParentId doğrulama
        if (request.ParentId.HasValue)
        {
            var parentExists = await _context.ParameterGroups
                .AnyAsync(pg => pg.Id == request.ParentId.Value, cancellationToken);
            
            if (!parentExists)
            {
                throw new ArgumentException("Üst grup bulunamadı.");
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
