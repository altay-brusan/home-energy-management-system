using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Auth.Queries.GetUserStatus;

public record GetUserStatusQuery : IRequest<UserStatusDto>;

public class GetUserStatusQueryHandler : IRequestHandler<GetUserStatusQuery, UserStatusDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetUserStatusQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<UserStatusDto> Handle(GetUserStatusQuery request, CancellationToken cancellationToken)
    {
        var userId = _user.Id ?? throw new UnauthorizedAccessException("User not authenticated");
        //TODO : SİTE ID EKLENSİN NULL OLABİLİR.
        // Kullanıcının site'larını kontrol et
        var site = await _context.Sites
            .FirstOrDefaultAsync(s => s.UserId == userId && !s.IsDeleted, cancellationToken);

        if (site == null)
        {
            return new UserStatusDto
            {
                Status = UserStatusType.NeedsSite,
                RedirectTo = "create_site",
                Message = "Site oluşturmanız gerekiyor"
            };
        }

        // Site'ın setup'ını kontrol et
        var setup = await _context.Setups
            .FirstOrDefaultAsync(s => s.SiteId == site.Id, cancellationToken);

        if (setup == null)
        {
            return new UserStatusDto
            {
                Status = UserStatusType.NeedsSetup,
                RedirectTo = "create_setup",
                Message = "Device bağlamanız gerekiyor",
                Data = new UserStatusDataDto
                {
                    SiteId = site.Id,
                    SiteName = site.Name
                }
            };
        }

        // Setup'ın device'ını kontrol et
        var setupDevice = await _context.SetupDevices
            .Include(sd => sd.Device)
            .FirstOrDefaultAsync(sd => sd.SetupId == setup.Id, cancellationToken);

        if (setupDevice == null)
        {
            return new UserStatusDto
            {
                Status = UserStatusType.NeedsSetup,
                RedirectTo = "create_setup",
                Message = "Device bağlamanız gerekiyor",
                Data = new UserStatusDataDto
                {
                    SiteId = site.Id,
                    SiteName = site.Name,
                    SetupId = setup.Id,
                    SetupName = setup.Name
                }
            };
        }

        // Her şey tamam - Dashboard'a yönlendir
        return new UserStatusDto
        {
            Status = UserStatusType.Complete,
            RedirectTo = "dashboard",
            Message = "Hoş geldiniz!",
            Data = new UserStatusDataDto
            {
                SiteId = site.Id,
                SiteName = site.Name,
                SetupId = setup.Id,
                SetupName = setup.Name,
                DeviceId = setupDevice.DeviceId,
                DeviceName = $"{setupDevice.Device.Manufacturer} {setupDevice.Device.ModelNumber}"
            }
        };
    }
}
