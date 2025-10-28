using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.Devices.Queries.CheckDeviceAccess;
using Hems_Backend.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Hems_Backend.Application.Dashboard.Queries.GetSOH;

public class GetSOHQueryHandler : IRequestHandler<GetSOHQuery, SOHResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ISender _sender;

    public GetSOHQueryHandler(IApplicationDbContext context, ISender sender)
    {
        _context = context;
        _sender = sender;
    }

    public async Task<SOHResponseDto> Handle(GetSOHQuery request, CancellationToken cancellationToken)
    {
        // Device access kontrolü
        var hasAccess = await _sender.Send(new CheckDeviceAccessQuery { DeviceId = request.DeviceId }, cancellationToken);
        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("Device access denied");
        }

        // Önce toplam veri miktarını kontrol et
        var totalDataCount = await _context.DeviceData
            .Where(x => x.DeviceId == request.DeviceId && 
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
            return new SOHResponseDto
            {
                Range = "Day",
                Data = new List<SOHDataPoint>(),
                Statistics = new SOHStatistics
                {
                    Peak = 0,
                    Average = 0,
                    Unit = "%"
                }
            };
        }

        // SOH değerlerini çıkar ve grupla
        var sohDataPoints = new List<SOHDataPoint>();
        
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
                    "Day" => data.RequestDate.ToString("HH:mm"),
                    "Week" => data.RequestDate.ToString("MM-dd"),
                    "Month" => data.RequestDate.ToString("MM-dd"),
                    "Year" => data.RequestDate.ToString("MM"),
                    _ => data.RequestDate.ToString("HH:mm")
                };
                
                sohDataPoints.Add(new SOHDataPoint
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
            return new SOHResponseDto
            {
                Range = "Day",
                Data = new List<SOHDataPoint>(),
                Statistics = new SOHStatistics
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
                .Select(g => new SOHDataPoint
                {
                    Timestamp = $"{g.Key}:00",
                    Value = Math.Round(g.Average(x => x.Value), 1),
                    Unit = "%"
                })
                .OrderBy(x => x.Timestamp)
                .ToList(),
            
            "Week" => sohDataPoints
                .Select(x => new SOHDataPoint
                {
                    Timestamp = x.Timestamp, // Zaten günlük veri
                    Value = x.Value,
                    Unit = "%"
                })
                .OrderBy(x => x.Timestamp)
                .ToList(),
            
            "Month" => sohDataPoints
                .Select(x => new SOHDataPoint
                {
                    Timestamp = x.Timestamp, // Zaten günlük veri
                    Value = x.Value,
                    Unit = "%"
                })
                .OrderBy(x => x.Timestamp)
                .ToList(),
            
            "Year" => sohDataPoints
                .Select(x => new SOHDataPoint
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

        return new SOHResponseDto
        {
            Range = range,
            Data = groupedData,
            Statistics = new SOHStatistics
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

    private async Task<List<Hems_Backend.Domain.Entities.DeviceData>> GetDataByRange(Guid deviceId, string range, CancellationToken cancellationToken)
    {
        return range switch
        {
            "Day" => await GetDayData(deviceId, cancellationToken),
            "Week" => await GetWeekData(deviceId, cancellationToken),
            "Month" => await GetMonthData(deviceId, cancellationToken),
            "Year" => await GetYearData(deviceId, cancellationToken),
            _ => await GetDayData(deviceId, cancellationToken)
        };
    }

    private async Task<List<Hems_Backend.Domain.Entities.DeviceData>> GetDayData(Guid deviceId, CancellationToken cancellationToken)
    {
        // Son 24 saatteki tüm veriler
        var startDate = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(-1), DateTimeKind.Utc);
        return await _context.DeviceData
            .Where(x => x.DeviceId == deviceId && 
                       x.RequestDate >= startDate &&
                       x.DataType == "BatteryData")
            .OrderBy(x => x.RequestDate)
            .ToListAsync(cancellationToken);
    }

    private async Task<List<Hems_Backend.Domain.Entities.DeviceData>> GetWeekData(Guid deviceId, CancellationToken cancellationToken)
    {
        // Son 7 günün her gününden son veri
        var result = new List<Hems_Backend.Domain.Entities.DeviceData>();
        
        for (int i = 6; i >= 0; i--)
        {
            var utcNow = DateTime.UtcNow.AddDays(-i);
            var startOfDay = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, DateTimeKind.Utc);
            var endOfDay = startOfDay.AddDays(1);
            
            var lastDataOfDay = await _context.DeviceData
                .Where(x => x.DeviceId == deviceId && 
                           x.RequestDate >= startOfDay &&
                           x.RequestDate < endOfDay &&
                           x.DataType == "BatteryData")
                .OrderByDescending(x => x.RequestDate)
                .FirstOrDefaultAsync(cancellationToken);
                
            if (lastDataOfDay != null)
            {
                result.Add(lastDataOfDay);
            }
        }
        
        return result.OrderBy(x => x.RequestDate).ToList();
    }

    private async Task<List<Hems_Backend.Domain.Entities.DeviceData>> GetMonthData(Guid deviceId, CancellationToken cancellationToken)
    {
        // Son 30 günün her gününden son veri
        var result = new List<Hems_Backend.Domain.Entities.DeviceData>();
        
        for (int i = 29; i >= 0; i--)
        {
            var utcNow = DateTime.UtcNow.AddDays(-i);
            var startOfDay = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, DateTimeKind.Utc);
            var endOfDay = startOfDay.AddDays(1);
            
            var lastDataOfDay = await _context.DeviceData
                .Where(x => x.DeviceId == deviceId && 
                           x.RequestDate >= startOfDay &&
                           x.RequestDate < endOfDay &&
                           x.DataType == "BatteryData")
                .OrderByDescending(x => x.RequestDate)
                .FirstOrDefaultAsync(cancellationToken);
                
            if (lastDataOfDay != null)
            {
                result.Add(lastDataOfDay);
            }
        }
        
        return result.OrderBy(x => x.RequestDate).ToList();
    }

    private async Task<List<Hems_Backend.Domain.Entities.DeviceData>> GetYearData(Guid deviceId, CancellationToken cancellationToken)
    {
        // Son 12 ayın her ayından son veri
        var result = new List<Hems_Backend.Domain.Entities.DeviceData>();
        
        for (int i = 11; i >= 0; i--)
        {
            var targetMonth = DateTime.UtcNow.AddMonths(-i);
            var startOfMonth = DateTime.SpecifyKind(new DateTime(targetMonth.Year, targetMonth.Month, 1), DateTimeKind.Utc);
            var endOfMonth = DateTime.SpecifyKind(startOfMonth.AddMonths(1), DateTimeKind.Utc);
            
            var lastDataOfMonth = await _context.DeviceData
                .Where(x => x.DeviceId == deviceId && 
                           x.RequestDate >= startOfMonth &&
                           x.RequestDate < endOfMonth &&
                           x.DataType == "BatteryData")
                .OrderByDescending(x => x.RequestDate)
                .FirstOrDefaultAsync(cancellationToken);
                
            if (lastDataOfMonth != null)
            {
                result.Add(lastDataOfMonth);
            }
        }
        
        return result.OrderBy(x => x.RequestDate).ToList();
    }

    private static SOHResponseDto CreateEmptyResponse()
    {
        return new SOHResponseDto
        {
            Range = "Day",
            Data = new List<SOHDataPoint>(),
            Statistics = new SOHStatistics
            {
                Peak = 0,
                Average = 0,
                Unit = "%"
            }
        };
    }
}
