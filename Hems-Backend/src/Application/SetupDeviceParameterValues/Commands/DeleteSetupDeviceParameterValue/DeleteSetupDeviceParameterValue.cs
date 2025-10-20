using Hems_Backend.Application.Common.Interfaces;
using MediatR;

namespace Hems_Backend.Application.SetupDeviceParameterValues.Commands.DeleteSetupDeviceParameterValue;

public record DeleteSetupDeviceParameterValueCommand : IRequest
{
    public Guid Id { get; init; }
}

public class DeleteSetupDeviceParameterValueCommandHandler : IRequestHandler<DeleteSetupDeviceParameterValueCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteSetupDeviceParameterValueCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteSetupDeviceParameterValueCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.SetupDeviceParameterValues
            .FindAsync([request.Id], cancellationToken);

        if (entity == null)
        {
            throw new ArgumentException("Kayıt bulunamadı.");
        }

        _context.SetupDeviceParameterValues.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
