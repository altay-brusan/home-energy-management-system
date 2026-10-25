using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.Devices.Queries.CheckDeviceAccess;
using Hems_Backend.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Hems_Backend.Application.Dashboard.Queries.GetSoh;

public class GetSohQueryHandler : IRequestHandler<GetSohQuery, SohResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ISender _sender;
    private readonly IDeviceMappingService _deviceMapping;

    public GetSohQueryHandler(IApplicationDbContext context, ISender sender, IDeviceMappingService deviceMapping)
    {
        _context = context;
        _sender = sender;
        _deviceMapping = deviceMapping;
    }

    public async Task<SohResponseDto> Handle(GetSohQuery request, CancellationToken cancellationToken)
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

        // Önce toplam veri miktarını kontrol et
        var totalDataCount = await _context.DeviceData
            .Where(x => x.ThingsBoardDeviceId == thingsBoardDeviceId.Value && 
                       x.DataType == "BatteryData")
            .CountAsync(cancellationToken);

        if (totalDataCount == 0)
        {
            return CreateEmptyResponse();
        }

        // Battery sayısını al
        var batteryCount = await _context.DeviceComponents
            .Where(f => f.DeviceId == request.DeviceId && 
                       f.DeviceType == Domain.Enums.DeviceType.Battery)
            .CountAsync(cancellationToken);

        // Eğer battery component yoksa, hesaplama için 1 kullan
        if (batteryCount == 0)
        {
            batteryCount = 1;
        }

        var range = DetermineRange(totalDataCount, batteryCount);
        
        // Range'e göre verileri çek
        var batteryData = await GetDataByRange(request.DeviceId, range, cancellationToken);

        if (!batteryData.Any())
        {
            return new SohResponseDto
            {
                Range = "Day",
                Data = new List<SohDataPoint>(),
                Statistics = new SohStatistics
                {
                    Peak = 0,
                    Average = 0,
                    Unit = "%"
                }
            };
        }

        // SOH değerlerini çıkar ve grupla
        var sohDataPoints = new List<SohDataPoint>();
        
        foreach (var data in batteryData)
        {
            try
            {
                // JSON'dan SOH değerini al (array olabilir)
                var sohElement = data.Data.RootElement.GetProperty("SOH");
                double sohValue;
                
                if (sohElement.ValueKind == JsonValueKind.Array)
                {
                    // Array ise ortalama al
                    var sohArray = sohElement.EnumerateArray()
                        .Select(x => x.GetDouble())
                        .ToList();
                    sohValue = sohArray.Any() ? sohArray.Average() : 0;
                }
                else
                {
                    // Tek değer ise direkt al
                    sohValue = sohElement.GetDouble();
                }
                
                // Range'e göre timestamp formatı belirle
                var timestamp = range switch
                {
                    "Day" => data.DeviceLocalTime.ToString("HH:mm"),
                    "Week" => data.DeviceLocalTime.ToString("MM-dd"),
                    "Month" => data.DeviceLocalTime.ToString("MM-dd"),
                    "Year" => data.DeviceLocalTime.ToString("MM"),
                    _ => data.DeviceLocalTime.ToString("HH:mm")
                };
                
                sohDataPoints.Add(new SohDataPoint
                {
                    Timestamp = timestamp,
                    Value = Math.Round(sohValue, 1),
                    Unit = "%"
                });
            }
            catch
            {
                // JSON parse hatası - skip
                continue;
            }
        }

        if (!sohDataPoints.Any())
        {
            return new SohResponseDto
            {
                Range = "Day",
                Data = new List<SohDataPoint>(),
                Statistics = new SohStatistics
                {
                    Peak = 0,
                    Average = 0,
                    Unit = "%"
                }
            };
        }

        // Range'e göre gruplama yap
        var groupedData = range switch
        {
            "Day" => sohDataPoints
                .GroupBy(x => x.Timestamp.Substring(0, 2)) // Saatlik gruplama
                .Select(g => new SohDataPoint
                {
                    Timestamp = $"{g.Key}:00",
                    Value = Math.Round(g.Average(x => x.Value), 1),
                    Unit = "%"
                })
                .OrderBy(x => x.Timestamp)
                .ToList(),
            
            "Week" => sohDataPoints
                .Select(x => new SohDataPoint
                {
                    Timestamp = x.Timestamp, // Zaten günlük veri
                    Value = x.Value,
                    Unit = "%"
                })
                .OrderBy(x => x.Timestamp)
                .ToList(),
            
            "Month" => sohDataPoints
                .Select(x => new SohDataPoint
                {
                    Timestamp = x.Timestamp, // Zaten günlük veri
                    Value = x.Value,
                    Unit = "%"
                })
                .OrderBy(x => x.Timestamp)
                .ToList(),
            
            "Year" => sohDataPoints
                .Select(x => new SohDataPoint
                {
                    Timestamp = x.Timestamp, // Zaten aylık veri
                    Value = x.Value,
                    Unit = "%"
                })
                .OrderBy(x => x.Timestamp)
                .ToList(),
            
            _ => sohDataPoints
        };

        // İstatistikleri hesapla
        var allValues = groupedData.Select(x => x.Value).ToList();
        var peak = allValues.Count > 0 ? allValues.Max() : 0;
        var average = allValues.Count > 0 ? allValues.Average() : 0;

        return new SohResponseDto
        {
            Range = range,
            Data = groupedData,
            Statistics = new SohStatistics
            {
                Peak = Math.Round(peak, 1),
                Average = Math.Round(average, 1),
                Unit = "%"
            }
        };
    }

    private static string DetermineRange(int totalDataCount, int batteryCount)
    {
        // 5 dakikada bir veri geldiği varsayımıyla hesapla
        const int recordsPerDayPerBattery = 288; // 24 saat × 12 (5 dakikada bir)
        
        // Birden fazla battery varsa, her battery için ayrı veri geliyor
        var recordsPerDay = recordsPerDayPerBattery * batteryCount;
        
        var daysOfData = totalDataCount / (double)recordsPerDay;
        
        return daysOfData switch
        {
            <= 1 => "Day",
            <= 7 => "Week", 
            <= 30 => "Month",
            _ => "Year"
        };
    }

    private async Task<List<Hems_Backend.Domain.Entities.DeviceData>> GetDataByRange(Guid thingsBoardDeviceId, string range, CancellationToken cancellationToken)
    {
        return range switch
        {
            "Day" => await GetDayData(thingsBoardDeviceId, cancellationToken),
            "Week" => await GetWeekData(thingsBoardDeviceId, cancellationToken),
            "Month" => await GetMonthData(thingsBoardDeviceId, cancellationToken),
            "Year" => await GetYearData(thingsBoardDeviceId, cancellationToken),
            _ => await GetDayData(thingsBoardDeviceId, cancellationToken)
        };
    }

    private async Task<List<Hems_Backend.Domain.Entities.DeviceData>> GetDayData(Guid thingsBoardDeviceId, CancellationToken cancellationToken)
    {
        // Son 24 saatteki tüm veriler
        var startDate = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(-1), DateTimeKind.Utc);
        return await _context.DeviceData
            .Where(x => x.ThingsBoardDeviceId == thingsBoardDeviceId && 
                       x.DeviceLocalTime >= startDate &&
                       x.DataType == "BatteryData")
            .OrderBy(x => x.DeviceLocalTime)
            .ToListAsync(cancellationToken);
    }

    private async Task<List<Hems_Backend.Domain.Entities.DeviceData>> GetWeekData(Guid thingsBoardDeviceId, CancellationToken cancellationToken)
    {
        // Son 7 günün her gününden son veri
        var result = new List<Hems_Backend.Domain.Entities.DeviceData>();
        
        for (int i = 6; i >= 0; i--)
        {
            var utcNow = DateTime.UtcNow.AddDays(-i);
            var startOfDay = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, DateTimeKind.Utc);
            var endOfDay = startOfDay.AddDays(1);
            
            var lastDataOfDay = await _context.DeviceData
                .Where(x => x.ThingsBoardDeviceId == thingsBoardDeviceId && 
                           x.DeviceLocalTime >= startOfDay &&
                           x.DeviceLocalTime < endOfDay &&
                           x.DataType == "BatteryData")
                .OrderByDescending(x => x.DeviceLocalTime)
                .FirstOrDefaultAsync(cancellationToken);
                
            if (lastDataOfDay != null)
            {
                result.Add(lastDataOfDay);
            }
        }
        
        return result.OrderBy(x => x.DeviceLocalTime).ToList();
    }

    private async Task<List<Hems_Backend.Domain.Entities.DeviceData>> GetMonthData(Guid thingsBoardDeviceId, CancellationToken cancellationToken)
    {
        // Son 30 günün her gününden son veri
        var result = new List<Hems_Backend.Domain.Entities.DeviceData>();
        
        for (int i = 29; i >= 0; i--)
        {
            var utcNow = DateTime.UtcNow.AddDays(-i);
            var startOfDay = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, DateTimeKind.Utc);
            var endOfDay = startOfDay.AddDays(1);
            
            var lastDataOfDay = await _context.DeviceData
                .Where(x => x.ThingsBoardDeviceId == thingsBoardDeviceId && 
                           x.DeviceLocalTime >= startOfDay &&
                           x.DeviceLocalTime < endOfDay &&
                           x.DataType == "BatteryData")
                .OrderByDescending(x => x.DeviceLocalTime)
                .FirstOrDefaultAsync(cancellationToken);
                
            if (lastDataOfDay != null)
            {
                result.Add(lastDataOfDay);
            }
        }
        
        return result.OrderBy(x => x.DeviceLocalTime).ToList();
    }

    private async Task<List<Hems_Backend.Domain.Entities.DeviceData>> GetYearData(Guid thingsBoardDeviceId, CancellationToken cancellationToken)
    {
        // Son 12 ayın her ayından son veri
        var result = new List<Hems_Backend.Domain.Entities.DeviceData>();
        
        for (int i = 11; i >= 0; i--)
        {
            var targetMonth = DateTime.UtcNow.AddMonths(-i);
            var startOfMonth = DateTime.SpecifyKind(new DateTime(targetMonth.Year, targetMonth.Month, 1), DateTimeKind.Utc);
            var endOfMonth = DateTime.SpecifyKind(startOfMonth.AddMonths(1), DateTimeKind.Utc);
            
            var lastDataOfMonth = await _context.DeviceData
                .Where(x => x.ThingsBoardDeviceId == thingsBoardDeviceId && 
                           x.DeviceLocalTime >= startOfMonth &&
                           x.DeviceLocalTime < endOfMonth &&
                           x.DataType == "BatteryData")
                .OrderByDescending(x => x.DeviceLocalTime)
                .FirstOrDefaultAsync(cancellationToken);
                
            if (lastDataOfMonth != null)
            {
                result.Add(lastDataOfMonth);
            }
        }
        
        return result.OrderBy(x => x.DeviceLocalTime).ToList();
    }

    private static SohResponseDto CreateEmptyResponse()
    {
        return new SohResponseDto
        {
            Range = "Day",
            Data = new List<SohDataPoint>(),
            Statistics = new SohStatistics
            {
                Peak = 0,
                Average = 0,
                Unit = "%"
            }
        };
    }
}
