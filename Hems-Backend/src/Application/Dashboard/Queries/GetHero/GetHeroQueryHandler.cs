using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.Devices.Queries.CheckDeviceAccess;
using Hems_Backend.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Dashboard.Queries.GetHero;

public class GetHeroQueryHandler : IRequestHandler<GetHeroQuery, HeroResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ISender _sender;
    private readonly IDeviceMappingService _deviceMapping;

    public GetHeroQueryHandler(IApplicationDbContext context, ISender sender, IDeviceMappingService deviceMapping)
    {
        _context = context;
        _sender = sender;
        _deviceMapping = deviceMapping;
    }

    public async Task<HeroResponseDto> Handle(GetHeroQuery request, CancellationToken cancellationToken)
    {
        // Device access kontrolü
        var hasAccess = await _sender.Send(new CheckDeviceAccessQuery { DeviceId = request.DeviceId }, cancellationToken);
        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("Device access denied");
        }

        // Map local DeviceId to ThingsBoardDeviceId
        var thingsBoardDeviceId = await _deviceMapping.GetThingsBoardDeviceIdAsync(request.DeviceId, cancellationToken);
        if (thingsBoardDeviceId == null)
        {
            throw new ArgumentException($"Device with ID {request.DeviceId} not found or has no ThingsBoard mapping.");
        }

        var utcNow = DateTime.UtcNow;
        var startOfDay = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, DateTimeKind.Utc);
        var endOfDay = startOfDay.AddDays(1);

        // Bugünün son hourly verisini al
        var lastHourlyData = await _context.DeviceEnergyData
            .Where(x => x.ThingsBoardDeviceId == thingsBoardDeviceId.Value && 
                       x.DataDateTime >= startOfDay &&
                       x.DataDateTime < endOfDay &&
                       x.DataType == "DailyCumulativeEnergyData")
            .OrderByDescending(x => x.DataDateTime)
            .FirstOrDefaultAsync(cancellationToken);

        if (lastHourlyData == null)
        {
            return new HeroResponseDto
            {
                Title = "Smart Energy Management",
                Subtitle = "Real-time monitoring and control",
                Metrics = new HeroMetrics
                {
                    TodayEnergy = 0,
                    CurrentPower = 0,
                    LastSync = "No data",
                    Unit = "kWh|W"
                }
            };
        }

        // Today Energy: Son verinin toplam enerji değeri
        var todayEnergy = (lastHourlyData.GridImportEnergyKWh ?? 0) + 
                         (lastHourlyData.GeneratorEnergyKWh ?? 0) + 
                         (lastHourlyData.PvTotalEnergyKWh ?? 0) + 
                         (lastHourlyData.BatteryChargeEnergyKWh ?? 0);

        // Current Power için bir önceki veriyi al
        var previousHourlyData = await _context.DeviceEnergyData
            .Where(x => x.ThingsBoardDeviceId == thingsBoardDeviceId.Value && 
                       x.DataDateTime >= startOfDay &&
                       x.DataDateTime < endOfDay &&
                       x.DataType == "DailyCumulativeEnergyData" &&
                       x.DataDateTime < lastHourlyData.DataDateTime)
            .OrderByDescending(x => x.DataDateTime)
            .FirstOrDefaultAsync(cancellationToken);

        double currentPower = 0;
        if (previousHourlyData != null)
        {
            var previousEnergy = (previousHourlyData.GridImportEnergyKWh ?? 0) + 
                               (previousHourlyData.GeneratorEnergyKWh ?? 0) + 
                               (previousHourlyData.PvTotalEnergyKWh ?? 0) + 
                               (previousHourlyData.BatteryChargeEnergyKWh ?? 0);

            // kWh farkını kW'ya çevir (1 saat = 1 kW)
            var energyDifference = todayEnergy - previousEnergy;
            currentPower = energyDifference * 1000; // kWh'yi W'ye çevir
        }

        // Last Sync hesapla
        var timeDiff = DateTime.UtcNow - lastHourlyData.DataDateTime;
        string lastSync = GetTimeAgoString(timeDiff);

        return new HeroResponseDto
        {
            Title = "Smart Energy Management",
            Subtitle = "Real-time monitoring and control",
            Metrics = new HeroMetrics
            {
                TodayEnergy = Math.Round(todayEnergy, 1),
                CurrentPower = Math.Round(currentPower, 0),
                LastSync = lastSync,
                Unit = "kWh|W"
            }
        };
    }

    private static string GetTimeAgoString(TimeSpan timeDiff)
    {
        if (timeDiff.TotalMinutes < 1)
            return "Just now";
        else if (timeDiff.TotalMinutes < 60)
            return $"{(int)timeDiff.TotalMinutes} min ago";
        else if (timeDiff.TotalHours < 24)
            return $"{(int)timeDiff.TotalHours} hour ago";
        else
            return $"{(int)timeDiff.TotalDays} day ago";
    }
}
