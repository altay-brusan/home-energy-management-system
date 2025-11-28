using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.Devices.Queries.CheckDeviceAccess;
using Hems_Backend.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Dashboard.Queries.GetEnergyConsumption;

public class GetEnergyConsumptionQueryHandler : IRequestHandler<GetEnergyConsumptionQuery, EnergyConsumptionResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ISender _sender;

    public GetEnergyConsumptionQueryHandler(IApplicationDbContext context, ISender sender)
    {
        _context = context;
        _sender = sender;
    }

    public async Task<EnergyConsumptionResponseDto> Handle(GetEnergyConsumptionQuery request, CancellationToken cancellationToken)
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

    private async Task<EnergyConsumptionResponseDto> GetDailyConsumptionAsync(Guid deviceId, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var startOfDay = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, DateTimeKind.Utc);
        var endOfDay = startOfDay.AddDays(1);

        var dailyData = await _context.DeviceEnergyData
            .Where(x => x.ThingsBoardDeviceId == deviceId && 
                       x.DataDateTime >= startOfDay &&
                       x.DataDateTime < endOfDay &&
                       x.DataType == "DailyCumulativeEnergyData")
            .OrderBy(x => x.DataDateTime)
            .ToListAsync(cancellationToken);

        var dataPoints = dailyData
            .GroupBy(x => x.DataDateTime.Hour)
            .Select(g => new Hems_Backend.Application.Common.Models.EnergyConsumptionDataPoint
            {
                Timestamp = $"{g.Key:00}:00",
                Value = Math.Round(g.OrderByDescending(x => x.DataDateTime).First().LoadEnergyKWh ?? 0, 3),
                Unit = "kWh"
            })
            .OrderBy(x => x.Timestamp)
            .ToList();

        // Statistics hesapla
        var total = dataPoints.Sum(x => x.Value);
        var peak = dataPoints.Count > 0 ? dataPoints.Max(x => x.Value) : 0;
        var average = dataPoints.Count > 0 ? dataPoints.Average(x => x.Value) : 0;

        return new EnergyConsumptionResponseDto
        {
            Range = "Day",
            Data = dataPoints,
            Statistics = new EnergyConsumptionStatistics
            {
                Total = Math.Round(total, 3),
                Peak = Math.Round(peak, 3),
                Average = Math.Round(average, 3),
                Unit = "kWh"
            }
        };
    }

    private async Task<EnergyConsumptionResponseDto> GetWeeklyConsumptionAsync(Guid deviceId, CancellationToken cancellationToken)
    {
        var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time")).Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek); // Pazartesi
        var todayUtc = DateTime.SpecifyKind(today, DateTimeKind.Utc);
        var weekStartUtc = DateTime.SpecifyKind(weekStart, DateTimeKind.Utc);

        var dataPoints = new List<Hems_Backend.Application.Common.Models.EnergyConsumptionDataPoint>();

        // Önceki günler için LifetimeEnergyData
        for (int i = 0; i < (int)today.DayOfWeek; i++)
        {
            var day = weekStart.AddDays(i);
            var startOfDay = new DateTime(day.Year, day.Month, day.Day, 0, 0, 0, DateTimeKind.Utc);
            var endOfDay = startOfDay.AddDays(1);

            var lifetimeData = await _context.DeviceEnergyData
                .Where(x => x.ThingsBoardDeviceId == deviceId && 
                           x.DataDateTime >= startOfDay &&
                           x.DataDateTime < endOfDay &&
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
                
                dataPoints.Add(new Hems_Backend.Application.Common.Models.EnergyConsumptionDataPoint
                {
                    Timestamp = dayName,
                    Value = Math.Round(lifetimeData.LoadEnergyKWh ?? 0, 3),
                    Unit = "kWh"
                });
            }
        }

        // Bugün için DailyCumulativeEnergyData (son hourly)
        var utcNow2 = DateTime.UtcNow;
        var startOfDay2 = new DateTime(utcNow2.Year, utcNow2.Month, utcNow2.Day, 0, 0, 0, DateTimeKind.Utc);
        var endOfDay2 = startOfDay2.AddDays(1);
        
        var todayData = await _context.DeviceEnergyData
            .Where(x => x.ThingsBoardDeviceId == deviceId && 
                       x.DataDateTime >= startOfDay2 &&
                       x.DataDateTime < endOfDay2 &&
                       x.DataType == "DailyCumulativeEnergyData")
            .OrderByDescending(x => x.DataDateTime)
            .FirstOrDefaultAsync(cancellationToken);

        if (todayData != null)
        {
            dataPoints.Add(new Hems_Backend.Application.Common.Models.EnergyConsumptionDataPoint
            {
                Timestamp = $"Day {today.Day}",
                Value = Math.Round(todayData.LoadEnergyKWh ?? 0, 3),
                Unit = "kWh"
            });
        }

        // Statistics hesapla
        var total = dataPoints.Sum(x => x.Value);
        var peak = dataPoints.Count > 0 ? dataPoints.Max(x => x.Value) : 0;
        var average = dataPoints.Count > 0 ? dataPoints.Average(x => x.Value) : 0;

        return new EnergyConsumptionResponseDto
        {
            Range = "Week",
            Data = dataPoints.OrderBy(x => x.Timestamp).ToList(),
            Statistics = new EnergyConsumptionStatistics
            {
                Total = Math.Round(total, 3),
                Peak = Math.Round(peak, 3),
                Average = Math.Round(average, 3),
                Unit = "kWh"
            }
        };
    }

    private async Task<EnergyConsumptionResponseDto> GetMonthlyConsumptionAsync(Guid deviceId, CancellationToken cancellationToken)
    {
        var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time")).Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var todayUtc = DateTime.SpecifyKind(today, DateTimeKind.Utc);
        var monthStartUtc = DateTime.SpecifyKind(monthStart, DateTimeKind.Utc);

        var dataPoints = new List<Hems_Backend.Application.Common.Models.EnergyConsumptionDataPoint>();

        // Ayın geçen günleri için LifetimeEnergyData
        for (int i = 0; i < today.Day; i++)
        {
            var day = monthStart.AddDays(i);
            var dayUtc = DateTime.SpecifyKind(day, DateTimeKind.Utc);

            if (day.Date == today.Date)
            {
                // Bugün için DailyCumulativeEnergyData'nın son kaydını al
                var dayStart = new DateTime(day.Year, day.Month, day.Day, 0, 0, 0, DateTimeKind.Utc);
                var dayEnd = dayStart.AddDays(1);
                
                var todayData = await _context.DeviceEnergyData
                    .Where(x => x.ThingsBoardDeviceId == deviceId && 
                               x.DataDateTime >= dayStart &&
                               x.DataDateTime < dayEnd &&
                               x.DataType == "DailyCumulativeEnergyData")
                    .OrderByDescending(x => x.DataDateTime)
                    .FirstOrDefaultAsync(cancellationToken);

                if (todayData != null)
                {
                    dataPoints.Add(new Hems_Backend.Application.Common.Models.EnergyConsumptionDataPoint
                    {
                        Timestamp = $"Day {day.Day}",
                        Value = Math.Round(todayData.LoadEnergyKWh ?? 0, 3),
                        Unit = "kWh"
                    });
                }
            }
            else
            {
                // Geçmiş günler için LifetimeEnergyData
                var dayStart = new DateTime(day.Year, day.Month, day.Day, 0, 0, 0, DateTimeKind.Utc);
                var dayEnd = dayStart.AddDays(1);
                
                var lifetimeData = await _context.DeviceEnergyData
                    .Where(x => x.ThingsBoardDeviceId == deviceId && 
                               x.DataDateTime >= dayStart &&
                               x.DataDateTime < dayEnd &&
                               x.DataType == "LifetimeEnergyData")
                    .FirstOrDefaultAsync(cancellationToken);

                if (lifetimeData != null)
                {
                    dataPoints.Add(new Hems_Backend.Application.Common.Models.EnergyConsumptionDataPoint
                    {
                        Timestamp = $"Day {day.Day}",
                        Value = Math.Round(lifetimeData.LoadEnergyKWh ?? 0, 3),
                        Unit = "kWh"
                    });
                }
            }
        }

        // Statistics hesapla
        var total = dataPoints.Sum(x => x.Value);
        var peak = dataPoints.Count > 0 ? dataPoints.Max(x => x.Value) : 0;
        var average = dataPoints.Count > 0 ? dataPoints.Average(x => x.Value) : 0;

        return new EnergyConsumptionResponseDto
        {
            Range = "Month",
            Data = dataPoints.OrderBy(x => x.Timestamp).ToList(),
            Statistics = new EnergyConsumptionStatistics
            {
                Total = Math.Round(total, 3),
                Peak = Math.Round(peak, 3),
                Average = Math.Round(average, 3),
                Unit = "kWh"
            }
        };
    }

    private async Task<EnergyConsumptionResponseDto> GetYearlyConsumptionAsync(Guid deviceId, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var yearStart = new DateTime(today.Year, 1, 1);
        var yearStartUtc = DateTime.SpecifyKind(yearStart, DateTimeKind.Utc);

        var monthlyData = await _context.DeviceEnergyData
            .Where(x => x.ThingsBoardDeviceId == deviceId && 
                       x.DataDateTime >= yearStartUtc &&
                       x.DataType == "LifetimeEnergyData")
            .ToListAsync();

        var dataPoints = monthlyData
            .GroupBy(x => x.DataDateTime.Month)
            .Select(g => new Hems_Backend.Application.Common.Models.EnergyConsumptionDataPoint
            {
                Timestamp = g.Key switch
                {
                    1 => "January",
                    2 => "February",
                    3 => "March",
                    4 => "April",
                    5 => "May",
                    6 => "June",
                    7 => "July",
                    8 => "August",
                    9 => "September",
                    10 => "October",
                    11 => "November",
                    12 => "December",
                    _ => $"Month {g.Key}"
                },
                Value = Math.Round(g.OrderByDescending(x => x.DataDateTime).First().LoadEnergyKWh ?? 0, 3),
                Unit = "kWh"
            })
            .OrderBy(x => x.Timestamp)
            .ToList();

        // Statistics hesapla
        var total = dataPoints.Sum(x => x.Value);
        var peak = dataPoints.Count > 0 ? dataPoints.Max(x => x.Value) : 0;
        var average = dataPoints.Count > 0 ? dataPoints.Average(x => x.Value) : 0;

        return new EnergyConsumptionResponseDto
        {
            Range = "Year",
            Data = dataPoints,
            Statistics = new EnergyConsumptionStatistics
            {
                Total = Math.Round(total, 3),
                Peak = Math.Round(peak, 3),
                Average = Math.Round(average, 3),
                Unit = "kWh"
            }
        };
    }

    private static List<Hems_Backend.Application.Common.Models.EnergyConsumptionDataPoint> OptimizeDailyDataPoints(List<Hems_Backend.Application.Common.Models.EnergyConsumptionDataPoint> dataPoints)
    {
        if (dataPoints.Count <= 12)
            return dataPoints;

        // Peak değerleri bul
        var peaks = dataPoints
            .OrderByDescending(x => x.Value)
            .Take(6)
            .ToList();

        // Kalan saatleri eşit aralıklarla seç
        var remainingHours = dataPoints
            .Where(x => !peaks.Any(p => p.Timestamp == x.Timestamp))
            .OrderBy(x => x.Timestamp)
            .ToList();

        var step = Math.Max(1, remainingHours.Count / 6);
        var selectedRemaining = remainingHours
            .Where((x, i) => i % step == 0)
            .Take(6)
            .ToList();

        var result = peaks.Concat(selectedRemaining)
            .OrderBy(x => x.Timestamp)
            .ToList();

        return result;
    }
}
