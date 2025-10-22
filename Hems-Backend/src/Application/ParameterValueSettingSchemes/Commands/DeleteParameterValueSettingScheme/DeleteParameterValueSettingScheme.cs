using Hems_Backend.Application.Common.Interfaces;
using MediatR;

namespace Hems_Backend.Application.ParameterValueSettingSchemes.Commands.DeleteParameterValueSettingScheme;

public record DeleteParameterValueSettingSchemeCommand : IRequest
{
    public Guid Id { get; init; }
}

public class DeleteParameterValueSettingSchemeCommandHandler : IRequestHandler<DeleteParameterValueSettingSchemeCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteParameterValueSettingSchemeCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteParameterValueSettingSchemeCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.ParameterValueSettingSchemes
            .FindAsync([request.Id], cancellationToken);

        if (entity == null)
        {
            throw new ArgumentException("Kayıt bulunamadı.");
        }

        _context.ParameterValueSettingSchemes.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
