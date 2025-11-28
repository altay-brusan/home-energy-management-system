using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Sites.Queries.GetSitesByUser;

public record GetSitesByUserQuery : IRequest<SitesDashboardDto>;

public class GetSitesByUserQueryHandler : IRequestHandler<GetSitesByUserQuery, SitesDashboardDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetSitesByUserQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<SitesDashboardDto> Handle(GetSitesByUserQuery request, CancellationToken cancellationToken)
    {
        var userId = _user.Id ?? throw new UnauthorizedAccessException("User not authenticated");
        var userSharedSiteIds = await _context.UserSites.Where(f => f.UserId == _user.Id).Select(f => f.SiteId).ToListAsync();
        var sites = await _context.Sites
            .Include(x => x.Setup!)
                .ThenInclude(s => s.SetupDevice)
            .Where(x => (x.UserId == userId || userSharedSiteIds.Contains(x.Id)) && !x.IsDeleted)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var totalpower = 0.0;
        var siteList = new List<SiteWithPowerDto>();
        
        foreach (var site in sites)
        {
            var homeEnergy = 0.0;
            var powerString = "0.00 kW";
            
            if (site.Setup?.SetupDevice != null)
            {
                var deviceId = site.Setup.SetupDevice.DeviceId;
                
                // DeviceEnergyData'dan son veriyi çek (hourly olanlarından)
                var utcNow = DateTime.UtcNow;
                var startOfDay = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, DateTimeKind.Utc);
                var endOfDay = startOfDay.AddDays(1);
                
                var lastEnergyData = await _context.DeviceEnergyData
                    .Where(x => x.ThingsBoardDeviceId == deviceId && 
                               x.DataType == "DailyCumulativeEnergyData" &&
                               x.DataDateTime >= startOfDay &&
                               x.DataDateTime < endOfDay)
                    .OrderByDescending(x => x.DataDateTime)
                    .FirstOrDefaultAsync(cancellationToken);
                
                if (lastEnergyData != null)
                {
                    
                    var sources = (lastEnergyData.PvTotalEnergyKWh ?? 0) + 
                                 (lastEnergyData.BatteryDischargeEnergyKWh ?? 0) + 
                                 (lastEnergyData.GridImportEnergyKWh ?? 0) + 
                                 (lastEnergyData.GeneratorEnergyKWh ?? 0);
                    
                  
                    
                    homeEnergy = sources;
                }
                
                powerString = homeEnergy.ToString("0.00") + " kW";
            }
            
            totalpower += homeEnergy;
            
            // Site'i SiteWithPowerDto'ya çevir
            siteList.Add(new SiteWithPowerDto
            {
                Id = site.Id,
                UserId = site.UserId,
                Name = site.Name,
                Country = site.Country,
                City = site.City,
                District = site.District,
                AddressLine = site.AddressLine,
                Latitude = site.Latitude,
                Longitude = site.Longitude,
                Timezone = site.Timezone,
                IsDeleted = site.IsDeleted,
                HasDevice = site.Setup?.SetupDevice != null,
                Created = site.Created.DateTime,
                CreatedBy = site.CreatedBy,
                LastModified = site.LastModified.DateTime,
                LastModifiedBy = site.LastModifiedBy,
                Power = powerString
            });
        }

        // TotalPower'ı string formatına çevir
        var totalPowerString = totalpower.ToString("0.00") + " kW";
        
        // Dashboard istatistiklerini hesapla
        var totalHomes = sites.Count;
        var totalDevices = sites.Count(x => x.Setup?.SetupDevice != null);

        return new SitesDashboardDto
        {
            TotalHomes = totalHomes,
            TotalDevices = totalDevices,
            TotalPower = totalPowerString,
            SiteList = siteList
        };
    }
}
