using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Devices.Queries.CheckDeviceAccess;
using Hems_Backend.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Dashboard.Queries.GetEnergyConsumption;

public class GetEnergyConsumptionQueryHandler : IRequestHandler<GetEnergyConsumptionQuery, EnergyConsumptionDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ISender _sender;

    public GetEnergyConsumptionQueryHandler(IApplicationDbContext context, ISender sender)
    {
        _context = context;
        _sender = sender;
    }

    public async Task<EnergyConsumptionDto> Handle(GetEnergyConsumptionQuery request, CancellationToken cancellationToken)
    {
        // Device access kontrolü
        var hasAccess = await _sender.Send(new CheckDeviceAccessQuery { DeviceId = request.DeviceId }, cancellationToken);
        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("Bu cihaza erişim yetkiniz bulunmamaktadır.");
        }

        return request.Period.ToLower() switch
        {
            "daily" => await GetDailyConsumptionAsync(request.DeviceId, cancellationToken),
            "weekly" => await GetWeeklyConsumptionAsync(request.DeviceId, cancellationToken),
            "monthly" => await GetMonthlyConsumptionAsync(request.DeviceId, cancellationToken),
            "yearly" => await GetYearlyConsumptionAsync(request.DeviceId, cancellationToken),
            _ => throw new ArgumentException("Geçersiz period. daily, weekly, monthly veya yearly olmalı.")
        };
    }

    private async Task<EnergyConsumptionDto> GetDailyConsumptionAsync(Guid deviceId, CancellationToken cancellationToken)
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
            .Select(g => new EnergyConsumptionDataPoint
            {
                DateTime = g.First().DataDateTime,
                LoadEnergyKWh = Math.Round(g.OrderByDescending(x => x.DataDateTime).First().LoadEnergyKWh ?? 0, 3),
                Label = $"Hour-{g.Key:00}:00"
            })
            .OrderBy(x => x.DateTime)
            .ToList();

        // Peak detection ve 12 saat optimizasyonu
        var optimizedPoints = OptimizeDailyDataPoints(dataPoints);

        return new EnergyConsumptionDto
        {
            Period = "daily",
            DataPoints = optimizedPoints
        };
    }

    private async Task<EnergyConsumptionDto> GetWeeklyConsumptionAsync(Guid deviceId, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek); // Pazartesi
        var todayUtc = DateTime.SpecifyKind(today, DateTimeKind.Utc);
        var weekStartUtc = DateTime.SpecifyKind(weekStart, DateTimeKind.Utc);

        var dataPoints = new List<EnergyConsumptionDataPoint>();

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
                
                dataPoints.Add(new EnergyConsumptionDataPoint
                {
                    DateTime = dayUtc,
                    LoadEnergyKWh = lifetimeData.LoadEnergyKWh ?? 0,
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
            dataPoints.Add(new EnergyConsumptionDataPoint
            {
                DateTime = todayUtc,
                LoadEnergyKWh = todayData.LoadEnergyKWh ?? 0,
                Label = "Day-Today"
            });
        }

        return new EnergyConsumptionDto
        {
            Period = "weekly",
            DataPoints = dataPoints.OrderBy(x => x.DateTime).ToList()
        };
    }

    private async Task<EnergyConsumptionDto> GetMonthlyConsumptionAsync(Guid deviceId, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var todayUtc = DateTime.SpecifyKind(today, DateTimeKind.Utc);
        var monthStartUtc = DateTime.SpecifyKind(monthStart, DateTimeKind.Utc);

        var dataPoints = new List<EnergyConsumptionDataPoint>();

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
                dataPoints.Add(new EnergyConsumptionDataPoint
                {
                    DateTime = dayUtc,
                    LoadEnergyKWh = lifetimeData.LoadEnergyKWh ?? 0,
                    Label = $"Day-{day.Day}"
                });
            }
        }

        return new EnergyConsumptionDto
        {
            Period = "monthly",
            DataPoints = dataPoints.OrderBy(x => x.DateTime).ToList()
        };
    }

    private async Task<EnergyConsumptionDto> GetYearlyConsumptionAsync(Guid deviceId, CancellationToken cancellationToken)
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
            .Select(g => new EnergyConsumptionDataPoint
            {
                DateTime = DateTime.SpecifyKind(new DateTime(today.Year, g.Key, 1), DateTimeKind.Utc),
                LoadEnergyKWh = Math.Round(g.OrderByDescending(x => x.DataDateTime).First().LoadEnergyKWh ?? 0, 3),
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

        return new EnergyConsumptionDto
        {
            Period = "yearly",
            DataPoints = dataPoints
        };
    }

    private static List<EnergyConsumptionDataPoint> OptimizeDailyDataPoints(List<EnergyConsumptionDataPoint> dataPoints)
    {
        if (dataPoints.Count <= 12)
            return dataPoints;

        // Peak değerleri bul
        var peaks = dataPoints
            .OrderByDescending(x => x.LoadEnergyKWh)
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
