using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Devices.Queries.CheckDeviceAccess;
using Hems_Backend.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Dashboard.Queries.GetEnergyProduction;

public class GetEnergyProductionQueryHandler : IRequestHandler<GetEnergyProductionQuery, EnergyProductionDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ISender _sender;

    public GetEnergyProductionQueryHandler(IApplicationDbContext context, ISender sender)
    {
        _context = context;
        _sender = sender;
    }

    public async Task<EnergyProductionDto> Handle(GetEnergyProductionQuery request, CancellationToken cancellationToken)
    {
        // Device access kontrolü
        var hasAccess = await _sender.Send(new CheckDeviceAccessQuery { DeviceId = request.DeviceId }, cancellationToken);
        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("Bu cihaza erişim yetkiniz bulunmamaktadır.");
        }

        return request.Period.ToLower() switch
        {
            "daily" => await GetDailyProductionAsync(request.DeviceId, cancellationToken),
            "weekly" => await GetWeeklyProductionAsync(request.DeviceId, cancellationToken),
            "monthly" => await GetMonthlyProductionAsync(request.DeviceId, cancellationToken),
            "yearly" => await GetYearlyProductionAsync(request.DeviceId, cancellationToken),
            _ => throw new ArgumentException("Geçersiz period. daily, weekly, monthly veya yearly olmalı.")
        };
    }

    private async Task<EnergyProductionDto> GetDailyProductionAsync(Guid deviceId, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var todayUtc = DateTime.SpecifyKind(today, DateTimeKind.Utc);

        var dailyData = await _context.DeviceEnergyData
            .Where(x => x.DeviceId == deviceId && 
                       x.DataDateTime.Date == todayUtc.Date &&
                       x.DataType == "DailyCumulativeEnergyData")
            .OrderBy(x => x.DataDateTime)
            .ToListAsync(cancellationToken);

        var dataPoints = dailyData
            .GroupBy(x => x.DataDateTime.Hour)
            .Select(g => new EnergyProductionDataPoint
            {
                DateTime = g.First().DataDateTime,
                TotalProductionKWh = Math.Round((g.OrderByDescending(x => x.DataDateTime).First().PvEnergyKWh ?? 0) + (g.OrderByDescending(x => x.DataDateTime).First().GeneratorEnergyKWh ?? 0), 3),
                PvEnergyKWh = Math.Round(g.OrderByDescending(x => x.DataDateTime).First().PvEnergyKWh ?? 0, 3),
                GeneratorEnergyKWh = Math.Round(g.OrderByDescending(x => x.DataDateTime).First().GeneratorEnergyKWh ?? 0, 3),
                Label = $"Hour-{g.Key:00}:00"
            })
            .OrderBy(x => x.DateTime)
            .ToList();

        // Peak detection ve 12 saat optimizasyonu
        var optimizedPoints = OptimizeDailyDataPoints(dataPoints);

        return new EnergyProductionDto
        {
            Period = "daily",
            DataPoints = optimizedPoints
        };
    }

    private async Task<EnergyProductionDto> GetWeeklyProductionAsync(Guid deviceId, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek); // Pazartesi
        var todayUtc = DateTime.SpecifyKind(today, DateTimeKind.Utc);
        var weekStartUtc = DateTime.SpecifyKind(weekStart, DateTimeKind.Utc);

        var dataPoints = new List<EnergyProductionDataPoint>();

        // Önceki günler için LifetimeEnergyData
        for (int i = 0; i < (int)today.DayOfWeek; i++)
        {
            var day = weekStart.AddDays(i);
            var dayUtc = DateTime.SpecifyKind(day, DateTimeKind.Utc);

            var lifetimeData = await _context.DeviceEnergyData
                .Where(x => x.DeviceId == deviceId && 
                           x.DataDateTime.Date == dayUtc.Date &&
                           x.DataType == "LifetimeEnergyData")
                .FirstOrDefaultAsync(cancellationToken);

            if (lifetimeData != null)
            {
                var dayName = day.DayOfWeek switch
                {
                    DayOfWeek.Monday => "Monday",
                    DayOfWeek.Tuesday => "Tuesday", 
                    DayOfWeek.Wednesday => "Wednesday",
                    DayOfWeek.Thursday => "Thursday",
                    DayOfWeek.Friday => "Friday",
                    DayOfWeek.Saturday => "Saturday",
                    DayOfWeek.Sunday => "Sunday",
                    _ => "Unknown"
                };
                
                dataPoints.Add(new EnergyProductionDataPoint
                {
                    DateTime = dayUtc,
                    TotalProductionKWh = (lifetimeData.PvEnergyKWh ?? 0) + (lifetimeData.GeneratorEnergyKWh ?? 0),
                    PvEnergyKWh = lifetimeData.PvEnergyKWh ?? 0,
                    GeneratorEnergyKWh = lifetimeData.GeneratorEnergyKWh ?? 0,
                    Label = $"Day-{dayName}"
                });
            }
        }

        // Bugün için DailyCumulativeEnergyData (son hourly)
        var todayData = await _context.DeviceEnergyData
            .Where(x => x.DeviceId == deviceId && 
                       x.DataDateTime.Date == todayUtc.Date &&
                       x.DataType == "DailyCumulativeEnergyData")
            .OrderByDescending(x => x.DataDateTime)
            .FirstOrDefaultAsync(cancellationToken);

        if (todayData != null)
        {
            dataPoints.Add(new EnergyProductionDataPoint
            {
                DateTime = todayUtc,
                TotalProductionKWh = (todayData.PvEnergyKWh ?? 0) + (todayData.GeneratorEnergyKWh ?? 0),
                PvEnergyKWh = todayData.PvEnergyKWh ?? 0,
                GeneratorEnergyKWh = todayData.GeneratorEnergyKWh ?? 0,
                Label = "Day-Today"
            });
        }

        return new EnergyProductionDto
        {
            Period = "weekly",
            DataPoints = dataPoints.OrderBy(x => x.DateTime).ToList()
        };
    }

    private async Task<EnergyProductionDto> GetMonthlyProductionAsync(Guid deviceId, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var todayUtc = DateTime.SpecifyKind(today, DateTimeKind.Utc);
        var monthStartUtc = DateTime.SpecifyKind(monthStart, DateTimeKind.Utc);

        var dataPoints = new List<EnergyProductionDataPoint>();

        // Ayın geçen günleri için LifetimeEnergyData
        for (int i = 0; i < today.Day; i++)
        {
            var day = monthStart.AddDays(i);
            var dayUtc = DateTime.SpecifyKind(day, DateTimeKind.Utc);

            var lifetimeData = await _context.DeviceEnergyData
                .Where(x => x.DeviceId == deviceId && 
                           x.DataDateTime.Date == dayUtc.Date &&
                           x.DataType == "LifetimeEnergyData")
                .FirstOrDefaultAsync(cancellationToken);

            if (lifetimeData != null)
            {
                dataPoints.Add(new EnergyProductionDataPoint
                {
                    DateTime = dayUtc,
                    TotalProductionKWh = (lifetimeData.PvEnergyKWh ?? 0) + (lifetimeData.GeneratorEnergyKWh ?? 0),
                    PvEnergyKWh = lifetimeData.PvEnergyKWh ?? 0,
                    GeneratorEnergyKWh = lifetimeData.GeneratorEnergyKWh ?? 0,
                    Label = $"Day-{day.Day}"
                });
            }
        }

        return new EnergyProductionDto
        {
            Period = "monthly",
            DataPoints = dataPoints.OrderBy(x => x.DateTime).ToList()
        };
    }

    private async Task<EnergyProductionDto> GetYearlyProductionAsync(Guid deviceId, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var yearStart = new DateTime(today.Year, 1, 1);
        var yearStartUtc = DateTime.SpecifyKind(yearStart, DateTimeKind.Utc);

        var monthlyData = await _context.DeviceEnergyData
            .Where(x => x.DeviceId == deviceId && 
                       x.DataDateTime >= yearStartUtc &&
                       x.DataType == "LifetimeEnergyData")
            .ToListAsync();

        var dataPoints = monthlyData
            .GroupBy(x => x.DataDateTime.Month)
            .Select(g => new EnergyProductionDataPoint
            {
                DateTime = DateTime.SpecifyKind(new DateTime(today.Year, g.Key, 1), DateTimeKind.Utc),
                TotalProductionKWh = Math.Round((g.OrderByDescending(x => x.DataDateTime).First().PvEnergyKWh ?? 0) + (g.OrderByDescending(x => x.DataDateTime).First().GeneratorEnergyKWh ?? 0), 3),
                PvEnergyKWh = Math.Round(g.OrderByDescending(x => x.DataDateTime).First().PvEnergyKWh ?? 0, 3),
                GeneratorEnergyKWh = Math.Round(g.OrderByDescending(x => x.DataDateTime).First().GeneratorEnergyKWh ?? 0, 3),
                Label = g.Key switch
                {
                    1 => "Month-January",
                    2 => "Month-February",
                    3 => "Month-March",
                    4 => "Month-April",
                    5 => "Month-May",
                    6 => "Month-June",
                    7 => "Month-July",
                    8 => "Month-August",
                    9 => "Month-September",
                    10 => "Month-October",
                    11 => "Month-November",
                    12 => "Month-December",
                    _ => $"Month-{g.Key}"
                }
            })
            .OrderBy(x => x.DateTime)
            .ToList();

        return new EnergyProductionDto
        {
            Period = "yearly",
            DataPoints = dataPoints
        };
    }

    private static List<EnergyProductionDataPoint> OptimizeDailyDataPoints(List<EnergyProductionDataPoint> dataPoints)
    {
        if (dataPoints.Count <= 12)
            return dataPoints;

        // Peak değerleri bul
        var peaks = dataPoints
            .OrderByDescending(x => x.TotalProductionKWh)
            .Take(6)
            .ToList();

        // Kalan saatleri eşit aralıklarla seç
        var remainingHours = dataPoints
            .Where(x => !peaks.Any(p => p.DateTime.Hour == x.DateTime.Hour))
            .OrderBy(x => x.DateTime.Hour)
            .ToList();

        var step = Math.Max(1, remainingHours.Count / 6);
        var selectedRemaining = remainingHours
            .Where((x, i) => i % step == 0)
            .Take(6)
            .ToList();

        var result = peaks.Concat(selectedRemaining)
            .OrderBy(x => x.DateTime)
            .ToList();

        return result;
    }
}