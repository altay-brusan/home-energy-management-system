using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Setups.Queries.GetSetupDevicesByUser;

public record GetSetupDevicesByUserQuery : IRequest<UserSetupDeviceDto?>
{
    public Guid SiteId { get; init; }
}

public class UserSetupDeviceDto
{
    public Guid SetupDeviceId { get; set; }
    public Guid SetupId { get; set; }
    public string SetupName { get; set; } = string.Empty;
    public Guid SiteId { get; set; }
    public string SiteName { get; set; } = string.Empty;
    public DeviceDto Device { get; set; } = null!;
    public List<ExternalDeviceDto> ExternalDevices { get; set; } = new();
}

public class GetSetupDevicesByUserQueryHandler : IRequestHandler<GetSetupDevicesByUserQuery, UserSetupDeviceDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetSetupDevicesByUserQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<UserSetupDeviceDto?> Handle(GetSetupDevicesByUserQuery request, CancellationToken cancellationToken)
    {
        var userId = _user.Id ?? throw new UnauthorizedAccessException("User not authenticated");

        return await _context.SetupDevices
            .Where(sd => sd.Setup.SiteId == request.SiteId && 
                        (sd.Setup.Site.UserId == userId || 
                         _context.UserSites.Any(us => us.SiteId == sd.Setup.SiteId && us.UserId == userId)))
            .Select(sd => new UserSetupDeviceDto
            {
                SetupDeviceId = sd.Id,
                SetupId = sd.SetupId,
                SetupName = sd.Setup.Name,
                SiteId = sd.Setup.SiteId,
                SiteName = sd.Setup.Site.Name,
                Device = new DeviceDto
                {
                    Id = sd.Device.Id,
                    Manufacturer = sd.Device.Manufacturer,
                    ModelNumber = sd.Device.ModelNumber,
                    SerialNumber = sd.Device.SerialNumber,
                    TypeNumber = sd.Device.TypeNumber,
                    QRCode = sd.Device.QRCode,
                    Specifications = sd.Device.Specifications,
                    IsDeleted = sd.Device.IsDeleted,
                    InstallationDate = sd.Device.InstallationDate,
                    CalibrationDate = sd.Device.CalibrationDate,
                    NextCheckoutDate = sd.Device.NextCheckoutDate,
                    WarrantyPeriod = sd.Device.WarrantyPeriod,
                    WarrantyUnit = sd.Device.WarrantyUnit,
                    WarrantyStartDate = sd.Device.WarrantyStartDate,
                    DeviceComponents = sd.Device.DeviceComponents
                        .Where(dc => !dc.IsDeleted)
                        .Select(dc => new DeviceComponentDto
                        {
                            Id = dc.Id,
                            DeviceId = dc.DeviceId,
                            DeviceType = dc.DeviceType,
                            Name = dc.Name,
                            Manufacturer = dc.Manufacturer,
                            ModelNumber = dc.ModelNumber,
                            SerialNumber = dc.SerialNumber,
                            Specifications = dc.Specifications,
                            WarrantyPeriod = dc.WarrantyPeriod,
                            WarrantyUnit = dc.WarrantyUnit,
                            WarrantyStartDate = dc.WarrantyStartDate,
                            IsDeleted = dc.IsDeleted,
                            Created = dc.Created.DateTime,
                            CreatedBy = dc.CreatedBy,
                            LastModified = dc.LastModified.DateTime,
                            LastModifiedBy = dc.LastModifiedBy
                        }).ToList(),
                    Created = sd.Device.Created.DateTime,
                    CreatedBy = sd.Device.CreatedBy,
                    LastModified = sd.Device.LastModified.DateTime,
                    LastModifiedBy = sd.Device.LastModifiedBy
                },
                ExternalDevices = sd.ExternalDevices
                    .Select(ed => new ExternalDeviceDto
                    {
                        Id = ed.Id,
                        SetupDeviceId = ed.SetupDeviceId,
                        Name = ed.Name,
                        DeviceType = ed.DeviceType,
                        Specifications = ed.Specifications,
                        Created = ed.Created.DateTime,
                        CreatedBy = ed.CreatedBy,
                        LastModified = ed.LastModified.DateTime,
                        LastModifiedBy = ed.LastModifiedBy
                    }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
