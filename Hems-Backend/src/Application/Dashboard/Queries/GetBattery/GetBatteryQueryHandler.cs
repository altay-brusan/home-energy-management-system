using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.Devices.Queries.CheckDeviceAccess;
using Hems_Backend.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Hems_Backend.Application.Dashboard.Queries.GetBattery;

public class GetBatteryQueryHandler : IRequestHandler<GetBatteryQuery, BatteryResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ISender _sender;
    private readonly IDeviceMappingService _deviceMapping;

    public GetBatteryQueryHandler(IApplicationDbContext context, ISender sender, IDeviceMappingService deviceMapping)
    {
        _context = context;
        _sender = sender;
        _deviceMapping = deviceMapping;
    }

    public async Task<BatteryResponseDto> Handle(GetBatteryQuery request, CancellationToken cancellationToken)
    {
        // Device access kontrolü
        var hasAccess = await _sender.Send(new CheckDeviceAccessQuery { DeviceId = request.DeviceId }, cancellationToken);
        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("Bu cihaza erişim yetkiniz bulunmamaktadır.");
        }

        // Map local DeviceId to ThingsBoardDeviceId
        var thingsBoardDeviceId = await _deviceMapping.GetThingsBoardDeviceIdAsync(request.DeviceId, cancellationToken);
        if (thingsBoardDeviceId == null)
        {
            throw new ArgumentException($"Device with ID {request.DeviceId} not found or has no ThingsBoard mapping.");
        }

        var today = DateTime.UtcNow.Date;
        var todayUtc = DateTime.SpecifyKind(today, DateTimeKind.Utc);

        // Bugünün BatteryClusterData verilerini çek
        var batteryData = await _context.DeviceData
            .Where(x => x.ThingsBoardDeviceId == thingsBoardDeviceId.Value && 
                       x.DeviceLocalTime.Date == todayUtc.Date &&
                       x.DataType == "BatteryClusterData")
            .OrderBy(x => x.DeviceLocalTime)
            .ToListAsync(cancellationToken);

        if (!batteryData.Any())
        {
            return new BatteryResponseDto
            {
                Range = "Day",
                Data = new List<Hems_Backend.Application.Common.Models.BatteryDataPoint>(),
                Statistics = new BatteryStatistics
                {
                    Average = 0,
                    Peak = 0,
                    Current = 0,
                    Unit = "%"
                }
            };
        }

        // JSON'dan SOC değerlerini çıkar
        var socDataPoints = new List<Hems_Backend.Application.Dashboard.Queries.GetBattery.BatteryDataPoint>();
        var allSocValues = new List<double>();

        foreach (var data in batteryData)
        {
            try
            {
                var jsonData = data.Data.RootElement;
                if (jsonData.TryGetProperty("Soc", out var socElement) && 
                    socElement.ValueKind == JsonValueKind.Number)
                {
                    var soc = Math.Round(socElement.GetDouble(), 3);
                    allSocValues.Add(soc);

                    socDataPoints.Add(new BatteryDataPoint
                    {
                        DateTime = data.DeviceLocalTime,
                        Soc = soc,
                        Label = data.DeviceLocalTime.ToString("HH:mm")
                    });
                }
            }
            catch (Exception)
            {
                // JSON parse hatası durumunda skip et
                continue;
            }
        }

        if (!socDataPoints.Any())
        {
            return new BatteryResponseDto
            {
                Range = "Day",
                Data = new List<Hems_Backend.Application.Common.Models.BatteryDataPoint>(),
                Statistics = new BatteryStatistics
                {
                    Average = 0,
                    Peak = 0,
                    Current = 0,
                    Unit = "%"
                }
            };
        }

        // Saatlik gruplama yap (EnergyProduction/Consumption gibi)
        var hourlyData = socDataPoints
            .GroupBy(x => x.DateTime.Hour)
            .Select(g => new Hems_Backend.Application.Common.Models.BatteryDataPoint
            {
                Timestamp = $"{g.Key:00}:00",
                Value = Math.Round(g.Max(x => x.Soc), 3), // Saatlik peak (en yüksek SOC)
                Unit = "%"
            })
            .OrderBy(x => x.Timestamp)
            .ToList();

        // İstatistikleri hesapla
        var AverageSoc = Math.Round(allSocValues.Average(), 3);
        var PeakSoc = Math.Round(allSocValues.Max(), 3);
        var CurrentSoc = Math.Round(allSocValues.Last(), 3);

        return new BatteryResponseDto
        {
            Range = "Day",
            Data = hourlyData,
            Statistics = new BatteryStatistics
            {
                Average = AverageSoc,
                Peak = PeakSoc,
                Current = CurrentSoc,
                Unit = "%"
            }
        };
    }

    
    private static List<BatteryDataPoint> FindPeaks(List<BatteryDataPoint> dataPoints)
    {
        var peaks = new List<BatteryDataPoint>();
        
        for (int i = 1; i < dataPoints.Count - 1; i++)
        {
            var current = dataPoints[i];
            var previous = dataPoints[i - 1];
            var next = dataPoints[i + 1];

            // Peak: önceki ve sonraki değerlerden büyük
            if (current.Soc > previous.Soc && current.Soc > next.Soc)
            {
                peaks.Add(current);
            }
        }

        // En yüksek değerleri de ekle
        var topValues = dataPoints
            .OrderByDescending(x => x.Soc)
            .Take(3)
            .ToList();

        peaks.AddRange(topValues);

        return peaks.Distinct().ToList();
    }

    private static List<BatteryDataPoint> FindValleys(List<BatteryDataPoint> dataPoints)
    {
        var valleys = new List<BatteryDataPoint>();
        
        for (int i = 1; i < dataPoints.Count - 1; i++)
        {
            var current = dataPoints[i];
            var previous = dataPoints[i - 1];
            var next = dataPoints[i + 1];

            // Valley: önceki ve sonraki değerlerden küçük
            if (current.Soc < previous.Soc && current.Soc < next.Soc)
            {
                valleys.Add(current);
            }
        }

        // En düşük değerleri de ekle
        var bottomValues = dataPoints
            .OrderBy(x => x.Soc)
            .Take(3)
            .ToList();

        valleys.AddRange(bottomValues);

        return valleys.Distinct().ToList();
    }
}
