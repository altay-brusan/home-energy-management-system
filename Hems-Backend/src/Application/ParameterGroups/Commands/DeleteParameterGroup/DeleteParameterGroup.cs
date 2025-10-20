using Hems_Backend.Application.Common.Interfaces;
using MediatR;

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
            throw new ArgumentException("Kayıt bulunamadı.");
        }

        // Alt grup kontrolü
        var hasChildren = await _context.ParameterGroups
            .AnyAsync(pg => pg.ParentId == request.Id, cancellationToken);

        if (hasChildren)
        {
            throw new InvalidOperationException("Üst kayıtta alt kayıt bulunduğu için silme işlemi gerçekleştirilemedi.");
        }

        // Parameters tablosunda referans kontrolü
        var hasParameters = await _context.Parameters
            .AnyAsync(p => p.ParameterGroupId == request.Id, cancellationToken);

        if (hasParameters)
        {
            throw new InvalidOperationException("Bu gruba ait parametreler bulunduğu için silme işlemi gerçekleştirilemedi.");
        }

        _context.ParameterGroups.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
