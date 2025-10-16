using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.Devices.Queries.CheckDeviceAccess;
using Hems_Backend.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Hems_Backend.Application.Dashboard.Queries.GetSOC;

public class GetSOCQueryHandler : IRequestHandler<GetSOCQuery, SOCResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ISender _sender;

    public GetSOCQueryHandler(IApplicationDbContext context, ISender sender)
    {
        _context = context;
        _sender = sender;
    }

    public async Task<SOCResponseDto> Handle(GetSOCQuery request, CancellationToken cancellationToken)
    {
        // Device access kontrolü
        var hasAccess = await _sender.Send(new CheckDeviceAccessQuery { DeviceId = request.DeviceId }, cancellationToken);
        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("Device access denied");
        }

        var today = DateTime.UtcNow.Date;
        var todayUtc = DateTime.SpecifyKind(today, DateTimeKind.Utc);

        // Bugünün BatteryCluster verilerini al
        var batteryData = await _context.DeviceData
            .Where(x => x.DeviceId == request.DeviceId && 
                       x.RequestDate.Date == todayUtc.Date &&
                       x.DataType == "BatteryClusterData")
            .OrderBy(x => x.RequestDate)
            .ToListAsync(cancellationToken);

        if (!batteryData.Any())
        {
            return new SOCResponseDto
            {
                Range = "Day",
                Data = new List<SOCDataPoint>(),
                Statistics = new SOCStatistics
                {
                    Total = 0,
                    Peak = 0,
                    Average = 0,
                    Unit = "%"
                }
            };
        }

        // SOC değerlerini çıkar ve saatlik grupla
        var socDataPoints = new List<SOCDataPoint>();
        
        foreach (var data in batteryData)
        {
            try
            {
                // JSON'dan direkt SOC değerini al
                var socValue = data.Data.RootElement.GetProperty("SOC").GetDouble();
                
                socDataPoints.Add(new SOCDataPoint
                {
                    Timestamp = data.RequestDate.ToString("HH:mm"),
                    Value = Math.Round(socValue, 1),
                    Unit = "%"
                });
            }
            catch
            {
                // JSON parse hatası - skip
                continue;
            }
        }

        if (!socDataPoints.Any())
        {
            return new SOCResponseDto
            {
                Range = "Day",
                Data = new List<SOCDataPoint>(),
                Statistics = new SOCStatistics
                {
                    Total = 0,
                    Peak = 0,
                    Average = 0,
                    Unit = "%"
                }
            };
        }

        // Saatlik gruplama yap (en yüksek peak'i al)
        var hourlyData = socDataPoints
            .GroupBy(x => x.Timestamp.Substring(0, 2)) // Saat kısmını al (HH:mm -> HH)
            .Select(g => new SOCDataPoint
            {
                Timestamp = $"{g.Key}:00",
                Value = Math.Round(g.Max(x => x.Value), 1), // Saatlik peak
                Unit = "%"
            })
            .OrderBy(x => x.Timestamp)
            .ToList();

        // İstatistikleri hesapla
        var allValues = hourlyData.Select(x => x.Value).ToList();
        var total = allValues.Sum();
        var peak = allValues.Count > 0 ? allValues.Max() : 0;
        var average = allValues.Count > 0 ? allValues.Average() : 0;

        return new SOCResponseDto
        {
            Range = "Day",
            Data = hourlyData,
            Statistics = new SOCStatistics
            {
                Total = Math.Round(total, 1),
                Peak = Math.Round(peak, 1),
                Average = Math.Round(average, 1),
                Unit = "%"
            }
        };
    }
}
