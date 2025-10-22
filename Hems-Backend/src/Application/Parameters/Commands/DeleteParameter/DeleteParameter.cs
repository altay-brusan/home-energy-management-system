using Hems_Backend.Application.Common.Interfaces;
using MediatR;

namespace Hems_Backend.Application.Parameters.Commands.DeleteParameter;

public record DeleteParameterCommand : IRequest
{
    public Guid Id { get; init; }
}

public class DeleteParameterCommandHandler : IRequestHandler<DeleteParameterCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteParameterCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteParameterCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Parameters
            .FindAsync([request.Id], cancellationToken);

        if (entity == null)
        {
            throw new ArgumentException("Kayıt bulunamadı.");
        }

        // SetupDeviceParameterValues tablosunda referans kontrolü
        var hasSetupDeviceParameterValues = await _context.SetupDeviceParameterValues
            .AnyAsync(sdpv => sdpv.ParameterId == request.Id, cancellationToken);

        if (hasSetupDeviceParameterValues)
        {
            throw new InvalidOperationException("Bu parametre kullanıldığı için silme işlemi gerçekleştirilemedi.");
        }

        // ParameterValueSettingScheme tablosunda referans kontrolü
        var hasParameterValueSettingSchemes = await _context.ParameterValueSettingSchemes
            .AnyAsync(pvss => pvss.ParameterId == request.Id, cancellationToken);

        if (hasParameterValueSettingSchemes)
        {
            throw new InvalidOperationException("Bu parametre şemada kullanıldığı için silme işlemi gerçekleştirilemedi.");
        }

        _context.Parameters.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
