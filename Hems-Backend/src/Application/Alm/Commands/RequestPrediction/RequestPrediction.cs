using System.Globalization;
using System.Text;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Domain.Entities;
using Hems_Backend.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Application.Alm.Commands.RequestPrediction;

public record RequestPredictionCommand : IRequest<RequestPredictionResult>
{
    public Guid DeviceId { get; init; }
    public AlmModelType ModelType { get; init; } = AlmModelType.PowerConsumption;
}

public class RequestPredictionResult
{
    public bool Success { get; set; }
    public Guid? JobId { get; set; }
    public string? AlmJobId { get; set; }
    public bool FromCache { get; set; }
    public string? Error { get; set; }
}

internal sealed class RequestPredictionCommandHandler : IRequestHandler<RequestPredictionCommand, RequestPredictionResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IDeviceMappingService _deviceMapping;
    private readonly ITimezoneService _timezoneService;
    private readonly IAlmService _almService;
    private readonly IUser _user;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RequestPredictionCommandHandler> _logger;

    private const int DefaultDataWindowDays = 10;

    public RequestPredictionCommandHandler(
        IApplicationDbContext context,
        IDeviceMappingService deviceMapping,
        ITimezoneService timezoneService,
        IAlmService almService,
        IUser user,
        IConfiguration configuration,
        ILogger<RequestPredictionCommandHandler> logger)
    {
        _context = context;
        _deviceMapping = deviceMapping;
        _timezoneService = timezoneService;
        _almService = almService;
        _user = user;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<RequestPredictionResult> Handle(RequestPredictionCommand request, CancellationToken cancellationToken)
    {
        // 0. Get model configuration
        var modelConfig = AlmModelConfiguration.GetConfig(request.ModelType);

        // 1. Check for today's cache - return existing completed job if prediction data actually exists
        var todayUtc = DateTime.UtcNow.Date;

        // Find a completed job for today that has actual prediction history data
        var cachedJobWithData = await _context.AlmPredictionJobs
            .Where(j => j.DeviceId == request.DeviceId
                && j.ModelType == request.ModelType
                && j.Status == AlmJobStatus.Completed
                && j.Created >= todayUtc)
            .OrderByDescending(j => j.Created)
            .Select(j => new
            {
                Job = j,
                HasPredictionData = _context.AlmPredictionHistories
                    .Any(h => h.AlmPredictionJobId == j.Id)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (cachedJobWithData != null && cachedJobWithData.HasPredictionData)
        {
            _logger.LogInformation(
                "Returning cached prediction job for device {DeviceId}, model {ModelType}. JobId: {JobId}",
                request.DeviceId, request.ModelType, cachedJobWithData.Job.Id);

            return new RequestPredictionResult
            {
                Success = true,
                JobId = cachedJobWithData.Job.Id,
                AlmJobId = cachedJobWithData.Job.AlmJobId,
                FromCache = true
            };
        }

        // 2. Map DeviceId to ThingsBoardDeviceId
        var thingsBoardDeviceId = await _deviceMapping.GetThingsBoardDeviceIdAsync(request.DeviceId, cancellationToken);
        if (thingsBoardDeviceId == null)
        {
            return new RequestPredictionResult { Success = false, Error = $"Device not found: {request.DeviceId}" };
        }

        // 3. Get Site info for latitude/longitude
        var siteInfo = await _context.SetupDevices
            .Where(sd => sd.DeviceId == request.DeviceId)
            .Select(sd => new { sd.Setup.Site.Latitude, sd.Setup.Site.Longitude })
            .FirstOrDefaultAsync(cancellationToken);

        if (siteInfo == null || siteInfo.Latitude == null || siteInfo.Longitude == null)
        {
            return new RequestPredictionResult { Success = false, Error = "Site location (latitude/longitude) not configured for this device" };
        }

        // 4. Get device timezone for timestamp conversion
        var deviceTimezone = await _timezoneService.GetDeviceTimezoneAsync(request.DeviceId, cancellationToken);
        var deviceNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, deviceTimezone);
        var dataStartTime = deviceNow.AddDays(-DefaultDataWindowDays);

        // 5. Query DevicePowerData for the last N days (all columns)
        var powerData = await _context.DevicePowerData
            .Where(p => p.ThingsBoardDeviceId == thingsBoardDeviceId.Value
                        && p.DeviceLocalTime >= dataStartTime
                        && p.DeviceLocalTime <= deviceNow
                        && p.DataType == "PowerFlow")
            .OrderBy(p => p.DeviceLocalTime)
            .ToListAsync(cancellationToken);

        if (powerData.Count == 0)
        {
            return new RequestPredictionResult { Success = false, Error = "No power data available for prediction" };
        }

        // 6. Build CSV data with all columns
        var csvData = BuildCsvData(powerData);

        // 7. Build schema using model configuration
        var schema = new AlmSchema
        {
            Id = request.DeviceId.ToString(),
            TimestampCol = "DeviceLocalTime",
            TimestampFormat = "%Y-%m-%d %H:%M:%S.%f",
            Latitude = (double)siteInfo.Latitude.Value,
            Longitude = (double)siteInfo.Longitude.Value,
            Features = new List<string> { modelConfig.DatabaseColumn },
            FeatureMapping = new Dictionary<string, string>
            {
                { modelConfig.FeatureName, modelConfig.DatabaseColumn }
            }
        };

        // 8. Build callback URL
        var baseUrl = _configuration["Alm:CallbackBaseUrl"]
                      ?? _configuration["Application:BaseUrl"]
                      ?? "https://hems-api-dev.oklidis.com.tr";
        var callbackUrl = $"{baseUrl.TrimEnd('/')}/api/Alm/callback";

        // 9. Create prediction job record
        var job = new AlmPredictionJob
        {
            DeviceId = request.DeviceId,
            ThingsBoardDeviceId = thingsBoardDeviceId.Value,
            ModelType = request.ModelType,
            Status = AlmJobStatus.Queued,
            DataPointCount = powerData.Count,
            DataStartTime = dataStartTime,
            DataEndTime = deviceNow,
            RequestedByUserId = _user.Id
        };
        _context.AlmPredictionJobs.Add(job);
        await _context.SaveChangesAsync(cancellationToken);

        // 10. Submit to ALM service with the model-specific endpoint
        try
        {
            var almResponse = await _almService.SubmitPredictionJobAsync(
                csvData, schema, callbackUrl, modelConfig.ModuleEndpoint, cancellationToken);

            if (!almResponse.Success)
            {
                job.Status = AlmJobStatus.Failed;
                job.ErrorMessage = almResponse.Error ?? "ALM service returned failure";
                job.CompletedAt = DateTimeOffset.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                return new RequestPredictionResult { Success = false, Error = almResponse.Error, JobId = job.Id };
            }

            job.AlmJobId = almResponse.JobId;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "ALM prediction job submitted. JobId: {JobId}, AlmJobId: {AlmJobId}, DeviceId: {DeviceId}, ModelType: {ModelType}, DataPoints: {Count}",
                job.Id, almResponse.JobId, request.DeviceId, request.ModelType, powerData.Count);

            return new RequestPredictionResult
            {
                Success = true,
                JobId = job.Id,
                AlmJobId = almResponse.JobId,
                FromCache = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit ALM prediction job for device {DeviceId}", request.DeviceId);
            job.Status = AlmJobStatus.Failed;
            job.ErrorMessage = ex.Message;
            job.CompletedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return new RequestPredictionResult { Success = false, Error = "Failed to submit prediction job", JobId = job.Id };
        }
    }

    private static string BuildCsvData(List<DevicePowerData> powerData)
    {
        var sb = new StringBuilder();

        // Header row
        sb.AppendLine("\"Id\",\"ThingsBoardDeviceId\",\"DeviceLocalTime\",\"MessageType\",\"DataType\",\"PhaseType\",\"PvTotalPowerW\",\"PvMppt1PowerW\",\"PvMppt2PowerW\",\"PvMppt3PowerW\",\"PvMppt4PowerW\",\"GridTotalPowerW\",\"LoadTotalPowerW\",\"BatteryPowerW\",\"GeneratorTotalPowerW\",\"Created\",\"CreatedBy\",\"LastModified\",\"LastModifiedBy\",\"Version\"");

        // Data rows
        foreach (var p in powerData)
        {
            var deviceLocalTime = p.DeviceLocalTime.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
            var created = p.Created.ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture);
            var lastModified = p.LastModified.ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture);

            sb.AppendLine($"{p.Id},\"{p.ThingsBoardDeviceId}\",{deviceLocalTime},{p.MessageType},{p.DataType},{p.PhaseType},{FormatDouble(p.PvTotalPowerW)},{FormatDouble(p.PvMppt1PowerW)},{FormatDouble(p.PvMppt2PowerW)},{FormatDouble(p.PvMppt3PowerW)},{FormatDouble(p.PvMppt4PowerW)},{FormatDouble(p.GridTotalPowerW)},{FormatDouble(p.LoadTotalPowerW)},{FormatDouble(p.BatteryPowerW)},{FormatDouble(p.GeneratorTotalPowerW)},{created},{p.CreatedBy ?? ""},{lastModified},{p.LastModifiedBy ?? ""},{p.Version}");
        }

        return sb.ToString().TrimEnd();
    }

    private static string FormatDouble(double? value)
    {
        return value.HasValue ? value.Value.ToString("F1", CultureInfo.InvariantCulture) : "";
    }
}
