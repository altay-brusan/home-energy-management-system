using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Devices.Queries.CheckDeviceAccess;
using Hems_Backend.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Hems_Backend.Application.Dashboard.Queries.GetBattery;

public class GetBatteryQueryHandler : IRequestHandler<GetBatteryQuery, BatteryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ISender _sender;

    public GetBatteryQueryHandler(IApplicationDbContext context, ISender sender)
    {
        _context = context;
        _sender = sender;
    }

    public async Task<BatteryDto> Handle(GetBatteryQuery request, CancellationToken cancellationToken)
    {
        // Device access kontrolü
        var hasAccess = await _sender.Send(new CheckDeviceAccessQuery { DeviceId = request.DeviceId }, cancellationToken);
        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("Bu cihaza erişim yetkiniz bulunmamaktadır.");
        }

        var today = DateTime.UtcNow.Date;
        var todayUtc = DateTime.SpecifyKind(today, DateTimeKind.Utc);

        // Bugünün BatteryClusterData verilerini çek
        var batteryData = await _context.DeviceData
            .Where(x => x.DeviceId == request.DeviceId && 
                       x.RequestDate.Date == todayUtc.Date &&
                       x.DataType == "BatteryClusterData")
            .OrderBy(x => x.RequestDate)
            .ToListAsync(cancellationToken);

        if (!batteryData.Any())
        {
            return new BatteryDto
            {
                DataPoints = new List<BatteryDataPoint>(),
                AverageSOC = 0,
                PeakSOC = 0,
                CurrentSOC = 0
            };
        }

        // JSON'dan SOC değerlerini çıkar
        var socDataPoints = new List<BatteryDataPoint>();
        var allSocValues = new List<double>();

        foreach (var data in batteryData)
        {
            try
            {
                var jsonData = data.Data.RootElement;
                if (jsonData.TryGetProperty("SOC", out var socElement) && 
                    socElement.ValueKind == JsonValueKind.Number)
                {
                    var soc = Math.Round(socElement.GetDouble(), 3);
                    allSocValues.Add(soc);

                    socDataPoints.Add(new BatteryDataPoint
                    {
                        DateTime = data.RequestDate,
                        SOC = soc,
                        Label = data.RequestDate.ToString("HH:mm")
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
            return new BatteryDto
            {
                DataPoints = new List<BatteryDataPoint>(),
                AverageSOC = 0,
                PeakSOC = 0,
                CurrentSOC = 0
            };
        }

        // Peak detection ile optimize et
        var optimizedPoints = OptimizeBatteryDataPoints(socDataPoints);

        // İstatistikleri hesapla
        var averageSOC = Math.Round(allSocValues.Average(), 3);
        var peakSOC = Math.Round(allSocValues.Max(), 3);
        var currentSOC = Math.Round(allSocValues.Last(), 3);

        return new BatteryDto
        {
            DataPoints = optimizedPoints,
            AverageSOC = averageSOC,
            PeakSOC = peakSOC,
            CurrentSOC = currentSOC
        };
    }

    private static List<BatteryDataPoint> OptimizeBatteryDataPoints(List<BatteryDataPoint> dataPoints)
    {
        if (dataPoints.Count <= 10)
            return dataPoints;

        var optimizedPoints = new List<BatteryDataPoint>();

        // Peak detection algoritması
        var peaks = FindPeaks(dataPoints);
        var valleys = FindValleys(dataPoints);

        // Peak ve valley'leri ekle (max 6 tane)
        optimizedPoints.AddRange(peaks.Take(3));
        optimizedPoints.AddRange(valleys.Take(3));

        // Kalan noktaları eşit aralıklarla seç (max 4 tane)
        var remainingPoints = dataPoints
            .Where(x => !peaks.Any(p => p.DateTime == x.DateTime) && 
                       !valleys.Any(v => v.DateTime == x.DateTime))
            .OrderBy(x => x.DateTime)
            .ToList();

        var step = Math.Max(1, remainingPoints.Count / 4); // Max 4 ek nokta
        var selectedRemaining = remainingPoints
            .Where((x, i) => i % step == 0)
            .Take(4)
            .ToList();

        optimizedPoints.AddRange(selectedRemaining);

        // Zaman sırasına göre sırala ve tekrar edenleri kaldır
        var result = optimizedPoints
            .OrderBy(x => x.DateTime)
            .GroupBy(x => x.DateTime)
            .Select(g => g.First())
            .ToList();

        // Eğer hala 10'dan fazlaysa, en önemli olanları seç
        if (result.Count > 10)
        {
            var sortedByImportance = result
                .OrderByDescending(x => Math.Abs(x.SOC - result.Average(p => p.SOC)))
                .Take(10)
                .OrderBy(x => x.DateTime)
                .ToList();
            
            return sortedByImportance;
        }

        return result;
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
            if (current.SOC > previous.SOC && current.SOC > next.SOC)
            {
                peaks.Add(current);
            }
        }

        // En yüksek değerleri de ekle
        var topValues = dataPoints
            .OrderByDescending(x => x.SOC)
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
            if (current.SOC < previous.SOC && current.SOC < next.SOC)
            {
                valleys.Add(current);
            }
        }

        // En düşük değerleri de ekle
        var bottomValues = dataPoints
            .OrderBy(x => x.SOC)
            .Take(3)
            .ToList();

        valleys.AddRange(bottomValues);

        return valleys.Distinct().ToList();
    }
}
