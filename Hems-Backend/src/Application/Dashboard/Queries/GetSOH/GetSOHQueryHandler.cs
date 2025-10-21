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

        var today = DateTime.UtcNow.Date;
        var todayUtc = DateTime.SpecifyKind(today, DateTimeKind.Utc);

        // Bugünün BatteryData verilerini al
        var batteryData = await _context.DeviceData
            .Where(x => x.DeviceId == request.DeviceId && 
                       x.RequestDate.Date == todayUtc.Date &&
                       x.DataType == "BatteryData")
            .OrderBy(x => x.RequestDate)
            .ToListAsync(cancellationToken);

        if (!batteryData.Any())
        {
            return new SOHResponseDto
            {
                Range = "Day",
                Data = new List<SOHDataPoint>(),
                Statistics = new SOHStatistics
                {
                    Total = 0,
                    Peak = 0,
                    Average = 0,
                    Unit = "%"
                }
            };
        }

        // SOH değerlerini çıkar ve saatlik grupla
        var sohDataPoints = new List<SOHDataPoint>();
        
        foreach (var data in batteryData)
        {
            try
            {
                // JSON'dan direkt SOH değerini al
                var sohValue = data.Data.RootElement.GetProperty("SOH").GetDouble();
                
                sohDataPoints.Add(new SOHDataPoint
                {
                    Timestamp = data.RequestDate.ToString("HH:mm"),
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
                    Total = 0,
                    Peak = 0,
                    Average = 0,
                    Unit = "%"
                }
            };
        }

        // Saatlik gruplama yap (ortalama değeri al)
        var hourlyData = sohDataPoints
            .GroupBy(x => x.Timestamp.Substring(0, 2)) // Saat kısmını al (HH:mm -> HH)
            .Select(g => new SOHDataPoint
            {
                Timestamp = $"{g.Key}:00",
                Value = Math.Round(g.Average(x => x.Value), 1), // Saatlik ortalama
                Unit = "%"
            })
            .OrderBy(x => x.Timestamp)
            .ToList();

        // İstatistikleri hesapla
        var allValues = hourlyData.Select(x => x.Value).ToList();
        var total = allValues.Sum();
        var peak = allValues.Count > 0 ? allValues.Max() : 0;
        var average = allValues.Count > 0 ? allValues.Average() : 0;

        return new SOHResponseDto
        {
            Range = "Day",
            Data = hourlyData,
            Statistics = new SOHStatistics
            {
                Total = Math.Round(total, 1),
                Peak = Math.Round(peak, 1),
                Average = Math.Round(average, 1),
                Unit = "%"
            }
        };
    }
}
