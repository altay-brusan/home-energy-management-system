using System.Globalization;
using System.Text;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Constants;
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
        // 1. Map DeviceId to ThingsBoardDeviceId
        var thingsBoardDeviceId = await _deviceMapping.GetThingsBoardDeviceIdAsync(request.DeviceId, cancellationToken);
        if (thingsBoardDeviceId == null)
        {
            return new RequestPredictionResult { Success = false, Error = $"Device not found: {request.DeviceId}" };
        }

        // 2. Get Site info for latitude/longitude
        var siteInfo = await _context.SetupDevices
            .Where(sd => sd.DeviceId == request.DeviceId)
            .Select(sd => new { sd.Setup.Site.Latitude, sd.Setup.Site.Longitude })
            .FirstOrDefaultAsync(cancellationToken);

        if (siteInfo == null || siteInfo.Latitude == null || siteInfo.Longitude == null)
        {
            return new RequestPredictionResult { Success = false, Error = "Site location (latitude/longitude) not configured for this device" };
        }

        // 3. Get device timezone for timestamp conversion
        var deviceTimezone = await _timezoneService.GetDeviceTimezoneAsync(request.DeviceId, cancellationToken);
        var deviceNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, deviceTimezone);
        var dataStartTime = deviceNow.AddDays(-DefaultDataWindowDays);

        // 4. Query DevicePowerData for the last N days (all columns)
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

        // 5. Build CSV data with all columns
        var csvData = BuildCsvData(powerData);

        // 6. Build schema
        var schema = new AlmSchema
        {
            Id = request.DeviceId.ToString(),
            TimestampCol = "DeviceLocalTime",
            TimestampFormat = "%Y-%m-%d %H:%M:%S.%f",
            Latitude = (double)siteInfo.Latitude.Value,
            Longitude = (double)siteInfo.Longitude.Value,
            Features = GetFeaturesForModelType(request.ModelType),
            FeatureMapping = GetFeatureMappingForModelType(request.ModelType)
        };

        // 7. Build callback URL
        var baseUrl = _configuration["Alm:CallbackBaseUrl"]
                      ?? _configuration["Application:BaseUrl"]
                      ?? "https://hems-api-dev.oklidis.com.tr";
        var callbackUrl = $"{baseUrl.TrimEnd('/')}/api/Alm/callback";

        // 8. Create prediction job record
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

        // 9. Submit to ALM service
        try
        {
            var almResponse = await _almService.SubmitPredictionJobAsync(
                csvData, schema, callbackUrl, cancellationToken);

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
                "ALM prediction job submitted. JobId: {JobId}, AlmJobId: {AlmJobId}, DeviceId: {DeviceId}, DataPoints: {Count}",
                job.Id, almResponse.JobId, request.DeviceId, powerData.Count);

            return new RequestPredictionResult
            {
                Success = true,
                JobId = job.Id,
                AlmJobId = almResponse.JobId
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

    private static List<string> GetFeaturesForModelType(AlmModelType modelType)
    {
        return modelType switch
        {
            AlmModelType.PowerConsumption => new List<string> { AlmFeatures.LoadTotalPowerW },
            _ => new List<string> { AlmFeatures.LoadTotalPowerW }
        };
    }

    private static Dictionary<string, string> GetFeatureMappingForModelType(AlmModelType modelType)
    {
        // Maps ALM model's internal field name to our data column name
        return modelType switch
        {
            AlmModelType.PowerConsumption => new Dictionary<string, string>
            {
                { "global_active_power", AlmFeatures.LoadTotalPowerW }
            },
            _ => new Dictionary<string, string>
            {
                { "global_active_power", AlmFeatures.LoadTotalPowerW }
            }
        };
    }
}
