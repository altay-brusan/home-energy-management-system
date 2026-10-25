using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.Devices.Queries.CheckDeviceAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Hems_Backend.Application.Dashboard.Queries.GetSocHistory;

public class GetSocHistoryQueryHandler : IRequestHandler<GetSocHistoryQuery, SocHistoryResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ISender _sender;
    private readonly IDeviceMappingService _deviceMapping;
    private readonly IDemoDataService _demoDataService;

    // Output interval in minutes (downsampled)
    private const int OutputIntervalMinutes = 15;

    public GetSocHistoryQueryHandler(
        IApplicationDbContext context,
        ISender sender,
        IDeviceMappingService deviceMapping,
        IDemoDataService demoDataService)
    {
        _context = context;
        _sender = sender;
        _deviceMapping = deviceMapping;
        _demoDataService = demoDataService;
    }

    public async Task<SocHistoryResponseDto> Handle(GetSocHistoryQuery request, CancellationToken cancellationToken)
    {
        // Device access check (uses local DeviceId)
        var hasAccess = await _sender.Send(new CheckDeviceAccessQuery { DeviceId = request.DeviceId }, cancellationToken);
        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("Device access denied");
        }

        // Check if this is a demo device - return simulated data
        var isDemoDevice = await _demoDataService.IsDemoDeviceAsync(request.DeviceId, cancellationToken);
        if (isDemoDevice)
        {
            return GetSimulatedSocHistory(request.DeviceId, request.Date);
        }

        // Map local DeviceId to ThingsBoardDeviceId
        var thingsBoardDeviceId = await _deviceMapping.GetThingsBoardDeviceIdAsync(request.DeviceId, cancellationToken);
        if (thingsBoardDeviceId == null)
        {
            throw new ArgumentException($"Device with ID {request.DeviceId} not found or has no ThingsBoard mapping.");
        }

        // Calculate start and end of day in device local time
        var startOfDay = new DateTime(request.Date.Year, request.Date.Month, request.Date.Day, 0, 0, 0, DateTimeKind.Unspecified);
        var endOfDay = startOfDay.AddDays(1);

        // Query SOC data from DeviceData table
        var socData = await _context.DeviceData
            .Where(dd => dd.ThingsBoardDeviceId == thingsBoardDeviceId.Value
                      && dd.DataType == "BatterySoc"
                      && dd.DeviceLocalTime >= startOfDay
                      && dd.DeviceLocalTime < endOfDay)
            .OrderBy(dd => dd.DeviceLocalTime)
            .ToListAsync(cancellationToken);

        if (!socData.Any())
        {
            return new SocHistoryResponseDto
            {
                Date = request.Date.ToString("yyyy-MM-dd"),
                DeviceId = request.DeviceId,
                Data = new List<SocHistoryDataPoint>()
            };
        }

        // Parse SOC values from JSON data (mainSocPercent only)
        var rawDataPoints = socData
            .Select(dd => new RawSocDataPoint
            {
                DeviceLocalTime = dd.DeviceLocalTime,
                SocPercent = ExtractMainSocPercent(dd.Data)
            })
            .Where(dp => dp.SocPercent.HasValue)
            .ToList();

        // Downsample to 15-minute intervals, taking most recent value for each interval
        var downsampledData = DownsampleToIntervals(rawDataPoints, startOfDay, OutputIntervalMinutes);

        // Interpolate missing 15-minute intervals
        var interpolatedData = InterpolateSocGaps(downsampledData, TimeSpan.FromMinutes(OutputIntervalMinutes));

        // Convert to response format
        var dataPoints = interpolatedData
            .Select(dp => new SocHistoryDataPoint
            {
                Timestamp = dp.DeviceLocalTime.ToString("HH:mm"),
                SocPercent = Math.Round(dp.SocPercent ?? 0, 1),
                IsInterpolated = dp.IsInterpolated
            })
            .ToList();

        return new SocHistoryResponseDto
        {
            Date = request.Date.ToString("yyyy-MM-dd"),
            DeviceId = request.DeviceId,
            Data = dataPoints
        };
    }

    /// <summary>
    /// Extract mainSocPercent from the JSON data
    /// Sample: {"kind": "BatterySocState", "mainSocPercent": 64, ...}
    /// Only uses mainSocPercent, does not fallback to channel SOC values
    /// </summary>
    private static double? ExtractMainSocPercent(JsonDocument data)
    {
        try
        {
            var root = data.RootElement;

            if (root.TryGetProperty("mainSocPercent", out var mainSocElement))
            {
                if (mainSocElement.ValueKind == JsonValueKind.Number)
                {
                    return mainSocElement.GetDouble();
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Downsample data to fixed intervals (e.g., 15 minutes)
    /// Groups samples by interval and takes the most recent value within each interval
    /// </summary>
    private static List<RawSocDataPoint> DownsampleToIntervals(
        List<RawSocDataPoint> data,
        DateTime startOfDay,
        int intervalMinutes)
    {
        if (!data.Any())
            return new List<RawSocDataPoint>();

        var result = new List<RawSocDataPoint>();

        // Group data points by their interval bucket
        var groupedByInterval = data
            .GroupBy(dp =>
            {
                // Calculate which interval this data point belongs to
                var minutesSinceStart = (dp.DeviceLocalTime - startOfDay).TotalMinutes;
                var intervalIndex = (int)(minutesSinceStart / intervalMinutes);
                return intervalIndex;
            })
            .OrderBy(g => g.Key);

        foreach (var group in groupedByInterval)
        {
            // Take the most recent sample within this interval
            var mostRecent = group
                .OrderByDescending(dp => dp.DeviceLocalTime)
                .First();

            // Snap the timestamp to the interval boundary
            var intervalStart = startOfDay.AddMinutes(group.Key * intervalMinutes);

            result.Add(new RawSocDataPoint
            {
                DeviceLocalTime = intervalStart,
                SocPercent = mostRecent.SocPercent,
                IsInterpolated = false
            });
        }

        return result;
    }

    /// <summary>
    /// Interpolate missing SOC data using linear interpolation
    /// When gaps exceed the expected sampling interval, fill with interpolated values
    /// </summary>
    private static List<RawSocDataPoint> InterpolateSocGaps(List<RawSocDataPoint> data, TimeSpan expectedInterval)
    {
        if (data.Count < 2)
            return data;

        var interpolated = new List<RawSocDataPoint>();

        for (int i = 0; i < data.Count; i++)
        {
            interpolated.Add(data[i]);

            // Check if there's a gap to the next sample
            if (i < data.Count - 1)
            {
                var current = data[i];
                var next = data[i + 1];
                var gap = next.DeviceLocalTime - current.DeviceLocalTime;

                // If gap is more than 1 interval, interpolate the missing intervals
                if (gap > expectedInterval && current.SocPercent.HasValue && next.SocPercent.HasValue)
                {
                    var missingIntervals = (int)(gap.TotalMinutes / expectedInterval.TotalMinutes) - 1;

                    // Create interpolated samples using linear interpolation
                    for (int j = 1; j <= missingIntervals; j++)
                    {
                        var fraction = (double)j / (missingIntervals + 1);
                        var interpolatedSoc = current.SocPercent.Value +
                            (next.SocPercent.Value - current.SocPercent.Value) * fraction;

                        interpolated.Add(new RawSocDataPoint
                        {
                            DeviceLocalTime = current.DeviceLocalTime.AddMinutes(expectedInterval.TotalMinutes * j),
                            SocPercent = interpolatedSoc,
                            IsInterpolated = true
                        });
                    }
                }
            }
        }

        return interpolated.OrderBy(x => x.DeviceLocalTime).ToList();
    }

    /// <summary>
    /// Generate simulated SOC history for demo devices (15-minute intervals)
    /// </summary>
    private static SocHistoryResponseDto GetSimulatedSocHistory(Guid deviceId, DateOnly date)
    {
        var dataPoints = new List<SocHistoryDataPoint>();
        var random = new Random(date.DayNumber); // Seed for consistent results per day

        // Generate data points every 15 minutes
        var startSoc = 50.0 + random.NextDouble() * 30; // Start between 50-80%
        var currentSoc = startSoc;

        for (int hour = 0; hour < 24; hour++)
        {
            for (int minute = 0; minute < 60; minute += OutputIntervalMinutes)
            {
                // Simulate SOC changes based on time of day
                double change;
                if (hour >= 6 && hour < 18) // Daytime - solar charging
                {
                    change = random.NextDouble() * 6 - 1.5; // Mostly charging (scaled for 15-min)
                }
                else // Nighttime - discharging
                {
                    change = random.NextDouble() * -4.5; // Mostly discharging (scaled for 15-min)
                }

                currentSoc = Math.Clamp(currentSoc + change, 10, 100);

                dataPoints.Add(new SocHistoryDataPoint
                {
                    Timestamp = $"{hour:D2}:{minute:D2}",
                    SocPercent = Math.Round(currentSoc, 1),
                    IsInterpolated = false
                });
            }
        }

        return new SocHistoryResponseDto
        {
            Date = date.ToString("yyyy-MM-dd"),
            DeviceId = deviceId,
            Data = dataPoints
        };
    }

    private class RawSocDataPoint
    {
        public DateTime DeviceLocalTime { get; set; }
        public double? SocPercent { get; set; }
        public bool IsInterpolated { get; set; }
    }
}
