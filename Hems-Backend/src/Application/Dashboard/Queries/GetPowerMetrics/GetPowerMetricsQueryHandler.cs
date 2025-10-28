using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.Devices.Queries.CheckDeviceAccess;
using Hems_Backend.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Dashboard.Queries.GetPowerMetrics;

public class GetPowerMetricsQueryHandler : IRequestHandler<GetPowerMetricsQuery, PowerMetricsResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ISender _sender;

    public GetPowerMetricsQueryHandler(IApplicationDbContext context, ISender sender)
    {
        _context = context;
        _sender = sender;
    }

    public async Task<PowerMetricsResponseDto> Handle(GetPowerMetricsQuery request, CancellationToken cancellationToken)
    {
        // Device access kontrolü
        var hasAccess = await _sender.Send(new CheckDeviceAccessQuery { DeviceId = request.DeviceId }, cancellationToken);
        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("Device access denied");
        }

        return request.Period.ToLower() switch
        {
            "daily" => await GetDailyPowerMetricsAsync(request.DeviceId, cancellationToken),
            "weekly" => await GetWeeklyPowerMetricsAsync(request.DeviceId, cancellationToken),
            "monthly" => await GetMonthlyPowerMetricsAsync(request.DeviceId, cancellationToken),
            "yearly" => await GetYearlyPowerMetricsAsync(request.DeviceId, cancellationToken),
            _ => await GetDailyPowerMetricsAsync(request.DeviceId, cancellationToken)
        };
    }

    private async Task<PowerMetricsResponseDto> GetDailyPowerMetricsAsync(Guid deviceId, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var startOfDay = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, DateTimeKind.Utc);
        var endOfDay = startOfDay.AddDays(1);

        var energyData = await _context.DeviceEnergyData
            .Where(x => x.DeviceId == deviceId && 
                       x.DataDateTime >= startOfDay &&
                       x.DataDateTime < endOfDay &&
                       x.DataType == "DailyCumulativeEnergyData")
            .OrderBy(x => x.DataDateTime)
            .ToListAsync(cancellationToken);

        if (!energyData.Any())
        {
            return new PowerMetricsResponseDto
            {
                Range = "Day",
                Data = new List<Hems_Backend.Application.Common.Models.PowerMetricsDataPoint>(),
                Statistics = new PowerMetricsStatistics
                {
                    AverageSolarPower = 0,
                    PeakSolarPower = 0,
                    AverageGridPower = 0,
                    PeakGridPower = 0,
                    AverageBatteryPower = 0,
                    PeakBatteryPower = 0,
                    AverageLoadPower = 0,
                    PeakLoadPower = 0,
                    Unit = "kW"
                }
            };
        }

        // Saatlik gruplama ve power hesaplama (son değerleri al)
        var dataPoints = energyData
            .GroupBy(x => x.DataDateTime.Hour)
            .Select(g => new Hems_Backend.Application.Common.Models.PowerMetricsDataPoint
            {
                Timestamp = $"{g.Key:00}:00",
                SolarPower = Math.Round(g.OrderByDescending(x => x.DataDateTime).First().PvEnergyKWh ?? 0, 3),
                GridPower = Math.Round((g.OrderByDescending(x => x.DataDateTime).First().GridImportEnergyKWh ?? 0) - 
                                      (g.OrderByDescending(x => x.DataDateTime).First().GridExportEnergyKWh ?? 0), 3),
                BatteryPower = Math.Round((g.OrderByDescending(x => x.DataDateTime).First().BatteryChargeEnergyKWh ?? 0) - 
                                         (g.OrderByDescending(x => x.DataDateTime).First().BatteryDischargeEnergyKWh ?? 0), 3),
                LoadPower = Math.Round(g.OrderByDescending(x => x.DataDateTime).First().LoadEnergyKWh ?? 0, 3),
                Unit = "kW"
            })
            .OrderBy(x => x.Timestamp)
            .ToList();

        // İstatistikleri hesapla
        var allSolarPowers = dataPoints.Select(x => x.SolarPower).ToList();
        var allGridPowers = dataPoints.Select(x => x.GridPower).ToList();
        var allBatteryPowers = dataPoints.Select(x => x.BatteryPower).ToList();
        var allLoadPowers = dataPoints.Select(x => x.LoadPower).ToList();

        return new PowerMetricsResponseDto
        {
            Range = "Day",
            Data = dataPoints,
            Statistics = new PowerMetricsStatistics
            {
                AverageSolarPower = allSolarPowers.Count > 0 ? Math.Round(allSolarPowers.Average(), 3) : 0,
                PeakSolarPower = allSolarPowers.Count > 0 ? Math.Round(allSolarPowers.Max(), 3) : 0,
                AverageGridPower = allGridPowers.Count > 0 ? Math.Round(allGridPowers.Average(), 3) : 0,
                PeakGridPower = allGridPowers.Count > 0 ? Math.Round(allGridPowers.Max(), 3) : 0,
                AverageBatteryPower = allBatteryPowers.Count > 0 ? Math.Round(allBatteryPowers.Average(), 3) : 0,
                PeakBatteryPower = allBatteryPowers.Count > 0 ? Math.Round(allBatteryPowers.Max(), 3) : 0,
                AverageLoadPower = allLoadPowers.Count > 0 ? Math.Round(allLoadPowers.Average(), 3) : 0,
                PeakLoadPower = allLoadPowers.Count > 0 ? Math.Round(allLoadPowers.Max(), 3) : 0,
                Unit = "kW"
            }
        };
    }

    private async Task<PowerMetricsResponseDto> GetWeeklyPowerMetricsAsync(Guid deviceId, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var todayUtc = DateTime.SpecifyKind(today, DateTimeKind.Utc);
        var weekStartUtc = DateTime.SpecifyKind(weekStart, DateTimeKind.Utc);

        var dataPoints = new List<Hems_Backend.Application.Common.Models.PowerMetricsDataPoint>();

        // Önceki günler için LifetimeEnergyData
        for (int i = 0; i < (int)today.DayOfWeek; i++)
        {
            var day = weekStart.AddDays(i);
            var dayStart = new DateTime(day.Year, day.Month, day.Day, 0, 0, 0, DateTimeKind.Utc);
            var dayEnd = dayStart.AddDays(1);

            var lifetimeData = await _context.DeviceEnergyData
                .Where(x => x.DeviceId == deviceId && 
                           x.DataDateTime >= dayStart &&
                           x.DataDateTime < dayEnd &&
                           x.DataType == "LifetimeEnergyData")
                .FirstOrDefaultAsync(cancellationToken);

            if (lifetimeData != null)
            {
                dataPoints.Add(new Hems_Backend.Application.Common.Models.PowerMetricsDataPoint
                {
                    Timestamp = $"Day {day.Day}",
                    SolarPower = Math.Round(lifetimeData.PvEnergyKWh ?? 0, 3),
                    GridPower = Math.Round((lifetimeData.GridImportEnergyKWh ?? 0) - (lifetimeData.GridExportEnergyKWh ?? 0), 3),
                    BatteryPower = Math.Round((lifetimeData.BatteryChargeEnergyKWh ?? 0) - (lifetimeData.BatteryDischargeEnergyKWh ?? 0), 3),
                    LoadPower = Math.Round(lifetimeData.LoadEnergyKWh ?? 0, 3),
                    Unit = "kW"
                });
            }
        }

        // Bugün için DailyCumulativeEnergyData
        var utcNow = DateTime.UtcNow;
        var startOfDay = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, DateTimeKind.Utc);
        var endOfDay = startOfDay.AddDays(1);
        
        var todayData = await _context.DeviceEnergyData
            .Where(x => x.DeviceId == deviceId && 
                       x.DataDateTime >= startOfDay &&
                       x.DataDateTime < endOfDay &&
                       x.DataType == "DailyCumulativeEnergyData")
            .OrderByDescending(x => x.DataDateTime)
            .FirstOrDefaultAsync(cancellationToken);

        if (todayData != null)
        {
            dataPoints.Add(new Hems_Backend.Application.Common.Models.PowerMetricsDataPoint
            {
                Timestamp = $"Day {today.Day}",
                SolarPower = Math.Round(todayData.PvEnergyKWh ?? 0, 3),
                GridPower = Math.Round((todayData.GridImportEnergyKWh ?? 0) - (todayData.GridExportEnergyKWh ?? 0), 3),
                BatteryPower = Math.Round((todayData.BatteryChargeEnergyKWh ?? 0) - (todayData.BatteryDischargeEnergyKWh ?? 0), 3),
                LoadPower = Math.Round(todayData.LoadEnergyKWh ?? 0, 3),
                Unit = "kW"
            });
        }

        // İstatistikleri hesapla
        var allSolarPowers = dataPoints.Select(x => x.SolarPower).ToList();
        var allGridPowers = dataPoints.Select(x => x.GridPower).ToList();
        var allBatteryPowers = dataPoints.Select(x => x.BatteryPower).ToList();
        var allLoadPowers = dataPoints.Select(x => x.LoadPower).ToList();

        return new PowerMetricsResponseDto
        {
            Range = "Week",
            Data = dataPoints.OrderBy(x => x.Timestamp).ToList(),
            Statistics = new PowerMetricsStatistics
            {
                AverageSolarPower = allSolarPowers.Count > 0 ? Math.Round(allSolarPowers.Average(), 3) : 0,
                PeakSolarPower = allSolarPowers.Count > 0 ? Math.Round(allSolarPowers.Max(), 3) : 0,
                AverageGridPower = allGridPowers.Count > 0 ? Math.Round(allGridPowers.Average(), 3) : 0,
                PeakGridPower = allGridPowers.Count > 0 ? Math.Round(allGridPowers.Max(), 3) : 0,
                AverageBatteryPower = allBatteryPowers.Count > 0 ? Math.Round(allBatteryPowers.Average(), 3) : 0,
                PeakBatteryPower = allBatteryPowers.Count > 0 ? Math.Round(allBatteryPowers.Max(), 3) : 0,
                AverageLoadPower = allLoadPowers.Count > 0 ? Math.Round(allLoadPowers.Average(), 3) : 0,
                PeakLoadPower = allLoadPowers.Count > 0 ? Math.Round(allLoadPowers.Max(), 3) : 0,
                Unit = "kW"
            }
        };
    }

    private async Task<PowerMetricsResponseDto> GetMonthlyPowerMetricsAsync(Guid deviceId, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var todayUtc = DateTime.SpecifyKind(today, DateTimeKind.Utc);
        var monthStartUtc = DateTime.SpecifyKind(monthStart, DateTimeKind.Utc);

        var dataPoints = new List<Hems_Backend.Application.Common.Models.PowerMetricsDataPoint>();

        // Ayın geçen günleri için LifetimeEnergyData
        for (int i = 0; i < today.Day; i++)
        {
            var day = monthStart.AddDays(i);
            var startOfDay = new DateTime(day.Year, day.Month, day.Day, 0, 0, 0, DateTimeKind.Utc);
            var endOfDay = startOfDay.AddDays(1);

            var lifetimeData = await _context.DeviceEnergyData
                .Where(x => x.DeviceId == deviceId && 
                           x.DataDateTime >= startOfDay &&
                           x.DataDateTime < endOfDay &&
                           x.DataType == "LifetimeEnergyData")
                .FirstOrDefaultAsync(cancellationToken);

            if (lifetimeData != null)
            {
                dataPoints.Add(new Hems_Backend.Application.Common.Models.PowerMetricsDataPoint
                {
                    Timestamp = $"Day {day.Day}",
                    SolarPower = Math.Round(lifetimeData.PvEnergyKWh ?? 0, 3),
                    GridPower = Math.Round((lifetimeData.GridImportEnergyKWh ?? 0) - (lifetimeData.GridExportEnergyKWh ?? 0), 3),
                    BatteryPower = Math.Round((lifetimeData.BatteryChargeEnergyKWh ?? 0) - (lifetimeData.BatteryDischargeEnergyKWh ?? 0), 3),
                    LoadPower = Math.Round(lifetimeData.LoadEnergyKWh ?? 0, 3),
                    Unit = "kW"
                });
            }
        }

        // İstatistikleri hesapla
        var allSolarPowers = dataPoints.Select(x => x.SolarPower).ToList();
        var allGridPowers = dataPoints.Select(x => x.GridPower).ToList();
        var allBatteryPowers = dataPoints.Select(x => x.BatteryPower).ToList();
        var allLoadPowers = dataPoints.Select(x => x.LoadPower).ToList();

        return new PowerMetricsResponseDto
        {
            Range = "Month",
            Data = dataPoints.OrderBy(x => x.Timestamp).ToList(),
            Statistics = new PowerMetricsStatistics
            {
                AverageSolarPower = allSolarPowers.Count > 0 ? Math.Round(allSolarPowers.Average(), 3) : 0,
                PeakSolarPower = allSolarPowers.Count > 0 ? Math.Round(allSolarPowers.Max(), 3) : 0,
                AverageGridPower = allGridPowers.Count > 0 ? Math.Round(allGridPowers.Average(), 3) : 0,
                PeakGridPower = allGridPowers.Count > 0 ? Math.Round(allGridPowers.Max(), 3) : 0,
                AverageBatteryPower = allBatteryPowers.Count > 0 ? Math.Round(allBatteryPowers.Average(), 3) : 0,
                PeakBatteryPower = allBatteryPowers.Count > 0 ? Math.Round(allBatteryPowers.Max(), 3) : 0,
                AverageLoadPower = allLoadPowers.Count > 0 ? Math.Round(allLoadPowers.Average(), 3) : 0,
                PeakLoadPower = allLoadPowers.Count > 0 ? Math.Round(allLoadPowers.Max(), 3) : 0,
                Unit = "kW"
            }
        };
    }

    private async Task<PowerMetricsResponseDto> GetYearlyPowerMetricsAsync(Guid deviceId, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var yearStart = new DateTime(today.Year, 1, 1);
        var todayUtc = DateTime.SpecifyKind(today, DateTimeKind.Utc);
        var yearStartUtc = DateTime.SpecifyKind(yearStart, DateTimeKind.Utc);

        var dataPoints = new List<Hems_Backend.Application.Common.Models.PowerMetricsDataPoint>();

        // Yılın geçen ayları için LifetimeEnergyData
        for (int i = 0; i < today.Month; i++)
        {
            var month = yearStart.AddMonths(i);
            var startOfMonth = new DateTime(month.Year, month.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var endOfMonth = startOfMonth.AddMonths(1);

            var lifetimeData = await _context.DeviceEnergyData
                .Where(x => x.DeviceId == deviceId && 
                           x.DataDateTime >= startOfMonth &&
                           x.DataDateTime < endOfMonth &&
                           x.DataType == "LifetimeEnergyData")
                .FirstOrDefaultAsync(cancellationToken);

            if (lifetimeData != null)
            {
                dataPoints.Add(new Hems_Backend.Application.Common.Models.PowerMetricsDataPoint
                {
                    Timestamp = $"Month {month.Month}",
                    SolarPower = Math.Round(lifetimeData.PvEnergyKWh ?? 0, 3),
                    GridPower = Math.Round((lifetimeData.GridImportEnergyKWh ?? 0) - (lifetimeData.GridExportEnergyKWh ?? 0), 3),
                    BatteryPower = Math.Round((lifetimeData.BatteryChargeEnergyKWh ?? 0) - (lifetimeData.BatteryDischargeEnergyKWh ?? 0), 3),
                    LoadPower = Math.Round(lifetimeData.LoadEnergyKWh ?? 0, 3),
                    Unit = "kW"
                });
            }
        }

        // İstatistikleri hesapla
        var allSolarPowers = dataPoints.Select(x => x.SolarPower).ToList();
        var allGridPowers = dataPoints.Select(x => x.GridPower).ToList();
        var allBatteryPowers = dataPoints.Select(x => x.BatteryPower).ToList();
        var allLoadPowers = dataPoints.Select(x => x.LoadPower).ToList();

        return new PowerMetricsResponseDto
        {
            Range = "Year",
            Data = dataPoints.OrderBy(x => x.Timestamp).ToList(),
            Statistics = new PowerMetricsStatistics
            {
                AverageSolarPower = allSolarPowers.Count > 0 ? Math.Round(allSolarPowers.Average(), 3) : 0,
                PeakSolarPower = allSolarPowers.Count > 0 ? Math.Round(allSolarPowers.Max(), 3) : 0,
                AverageGridPower = allGridPowers.Count > 0 ? Math.Round(allGridPowers.Average(), 3) : 0,
                PeakGridPower = allGridPowers.Count > 0 ? Math.Round(allGridPowers.Max(), 3) : 0,
                AverageBatteryPower = allBatteryPowers.Count > 0 ? Math.Round(allBatteryPowers.Average(), 3) : 0,
                PeakBatteryPower = allBatteryPowers.Count > 0 ? Math.Round(allBatteryPowers.Max(), 3) : 0,
                AverageLoadPower = allLoadPowers.Count > 0 ? Math.Round(allLoadPowers.Average(), 3) : 0,
                PeakLoadPower = allLoadPowers.Count > 0 ? Math.Round(allLoadPowers.Max(), 3) : 0,
                Unit = "kW"
            }
        };
    }
}