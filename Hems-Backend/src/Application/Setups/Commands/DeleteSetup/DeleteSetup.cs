using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Setups.Commands.DeleteSetup;

public record DeleteSetupCommand(Guid Id) : IRequest;

public class DeleteSetupCommandHandler : IRequestHandler<DeleteSetupCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public DeleteSetupCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(DeleteSetupCommand request, CancellationToken cancellationToken)
    {
        var userId = _user.Id ?? throw new UnauthorizedAccessException("User not authenticated");

        var entity = await _context.Setups
            .Include(s => s.Site)
            .FirstOrDefaultAsync(s => s.Id == request.Id && s.Site.UserId == userId, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        // Setup'ın SetupDevice'ı var mı kontrol et
        var hasSetupDevice = await _context.SetupDevices
            .AnyAsync(sd => sd.SetupId == request.Id, cancellationToken);

        if (hasSetupDevice)
        {
            throw new InvalidOperationException("Bu setup'ın bağlı device'ları var. Önce device bağlantısını kaldırmanız gerekiyor.");
        }

        _context.Setups.Remove(entity);

        await _context.SaveChangesAsync(cancellationToken);
    }
}








