using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Mobile.Queries.GetMobileDashboard;

public record GetMobileDashboardQuery : IRequest<MobileDashboardDto>;

public class GetMobileDashboardQueryHandler : IRequestHandler<GetMobileDashboardQuery, MobileDashboardDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetMobileDashboardQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<MobileDashboardDto> Handle(GetMobileDashboardQuery request, CancellationToken cancellationToken)
    {
        var userId = _user.Id ?? throw new UnauthorizedAccessException("User not authenticated");

        // Kullanıcının site'ını ve setup'ını getir
        var site = await _context.Sites
            .FirstOrDefaultAsync(s => s.UserId == userId && !s.IsDeleted, cancellationToken);

        if (site == null)
        {
            throw new InvalidOperationException("Kullanıcının site'ı bulunamadı");
        }

        var setup = await _context.Setups
            .FirstOrDefaultAsync(s => s.SiteId == site.Id, cancellationToken);

        if (setup == null)
        {
            throw new InvalidOperationException("Site'ın setup'ı bulunamadı");
        }

        // SetupDevice ve Device bilgilerini getir
        var setupDevice = await _context.SetupDevices
            .Include(sd => sd.Device)
            .ThenInclude(d => d.DeviceComponents.Where(dc => !dc.IsDeleted))
            .Include(sd => sd.ExternalDevices)
            .FirstOrDefaultAsync(sd => sd.SetupId == setup.Id, cancellationToken);

        if (setupDevice == null)
        {
            throw new InvalidOperationException("Setup'ın device'ı bulunamadı");
        }

        var device = setupDevice.Device;

        // Device Components'leri map et
        var deviceComponents = device.DeviceComponents.Select(dc => new DeviceComponentOverviewDto
        {
            Id = dc.Id,
            DeviceType = dc.DeviceType,
            Name = dc.Name,
            Manufacturer = dc.Manufacturer ?? string.Empty,
            ModelNumber = dc.ModelNumber ?? string.Empty,
            SerialNumber = dc.SerialNumber ?? string.Empty,
            Specifications = dc.Specifications,
            IsActive = !dc.IsDeleted,
            Warranty = CalculateWarrantyInfo(dc.WarrantyPeriod, dc.WarrantyUnit, dc.WarrantyStartDate)
        }).ToList();

        // External Devices'leri map et
        var externalDevices = setupDevice.ExternalDevices.Select(ed => new ExternalDeviceOverviewDto
        {
            Id = ed.Id,
            Name = ed.Name,
            DeviceType = ed.DeviceType,
            Specifications = ed.Specifications,
            IsActive = true, // External devices are always active
            Created = ed.Created.DateTime
        }).ToList();

        // Device Overview oluştur
        var deviceOverview = new DeviceOverviewDto
        {
            Id = device.Id,
            Name = $"{device.Manufacturer} {device.ModelNumber}",
            Manufacturer = device.Manufacturer,
            ModelNumber = device.ModelNumber ?? string.Empty,
            SerialNumber = device.SerialNumber ?? string.Empty,
            QRCode = device.QRCode ?? string.Empty,
            IsOnline = true, // TODO: Real-time status from DeviceSignal
            LastSeen = DateTime.UtcNow, // TODO: Real-time last seen
            Specifications = device.Specifications,
            Components = deviceComponents,
            Warranty = CalculateWarrantyInfo(device.WarrantyPeriod, device.WarrantyUnit, device.WarrantyStartDate)
        };

        // System Status oluştur (şimdilik mock data, gelecekte DeviceSignal'dan gelecek)
        var systemStatus = new SystemStatusDto
        {
            OverallStatus = "Normal",
            BatteryLevel = 85, // Mock data
            PowerGeneration = 1200.5, // Mock data
            PowerConsumption = 450.2, // Mock data
            GridImport = 0, // Mock data
            GridExport = 750.3, // Mock data
            OperatingMode = "Self-Consumption",
            BatteryTemperatures = new List<double> { 24.5, 28.2 }, // Mock data
            AmbientTemperature = 30.1, // Mock data
            FanState = "Low",
            LastUpdate = DateTime.UtcNow
        };

        // Active Alerts (şimdilik boş, gelecekte Alert entity'sinden gelecek)
        var activeAlerts = new List<AlertDto>();

        return new MobileDashboardDto
        {
            SiteId = site.Id,
            SiteName = site.Name,
            SetupId = setup.Id,
            SetupName = setup.Name,
            Device = deviceOverview,
            ExternalDevices = externalDevices,
            ActiveAlerts = activeAlerts,
            SystemStatus = systemStatus
        };
    }

    private static WarrantyInfoDto CalculateWarrantyInfo(int? warrantyPeriod, WarrantyUnit? warrantyUnit, DateTime? warrantyStartDate)
    {
        if (!warrantyPeriod.HasValue || !warrantyUnit.HasValue || !warrantyStartDate.HasValue)
        {
            return new WarrantyInfoDto
            {
                WarrantyPeriod = null,
                WarrantyUnit = null,
                WarrantyStartDate = null,
                WarrantyEndDate = null,
                DaysRemaining = 0,
                IsExpired = true
            };
        }

        var warrantyEndDate = warrantyUnit.Value switch
        {
            WarrantyUnit.Day => warrantyStartDate.Value.AddDays(warrantyPeriod.Value),
            WarrantyUnit.Month => warrantyStartDate.Value.AddMonths(warrantyPeriod.Value),
            WarrantyUnit.Year => warrantyStartDate.Value.AddYears(warrantyPeriod.Value),
            _ => warrantyStartDate.Value.AddYears(warrantyPeriod.Value)
        };

        var daysRemaining = (warrantyEndDate - DateTime.UtcNow).Days;
        var isExpired = daysRemaining <= 0;

        return new WarrantyInfoDto
        {
            WarrantyPeriod = warrantyPeriod.Value,
            WarrantyUnit = warrantyUnit.Value,
            WarrantyStartDate = warrantyStartDate.Value,
            WarrantyEndDate = warrantyEndDate,
            DaysRemaining = Math.Max(0, daysRemaining),
            IsExpired = isExpired
        };
    }
}
