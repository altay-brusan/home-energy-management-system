using Hems_Backend.Application.Common.Exceptions;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
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
        var userId = _user.Id
            ?? throw AppException.Unauthorized(MessageCodes.AUTH_USER_NOT_AUTHENTICATED);

        var entity = await _context.Setups
            .Include(s => s.Site)
            .FirstOrDefaultAsync(s => s.Id == request.Id &&
                                     (s.Site.UserId == userId ||
                                      _context.UserSites.Any(us => us.SiteId == s.SiteId && us.UserId == userId)),
                                 cancellationToken)
            ?? throw AppException.NotFound(
                MessageCodes.SETUP_NOT_FOUND,
                new Dictionary<string, object> { ["setupId"] = request.Id });

        // Check if setup has connected devices
        var hasSetupDevice = await _context.SetupDevices
            .AnyAsync(sd => sd.SetupId == request.Id, cancellationToken);

        if (hasSetupDevice)
        {
            throw AppException.BadRequest(
                MessageCodes.SETUP_CANNOT_DELETE_WITH_DEVICES,
                new Dictionary<string, object> { ["setupId"] = request.Id });
        }

        _context.Setups.Remove(entity);

        await _context.SaveChangesAsync(cancellationToken);
    }
}








