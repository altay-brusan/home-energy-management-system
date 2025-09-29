using System.Text.Json;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Application.DeviceData.Commands.BulkCreateDeviceData;

public record BulkCreateDeviceDataCommand : IRequest<BulkCreateDeviceDataResult>
{
    public List<DeviceDataItem> DeviceDataItems { get; init; } = new();
}

public record DeviceDataItem
{
    public string DataType { get; init; } = string.Empty;
    public Guid DeviceId { get; init; }
    public DateTime RequestDate { get; init; }
    public JsonDocument Data { get; init; } = JsonDocument.Parse("{}");
}

public record BulkCreateDeviceDataResult
{
    public int SuccessCount { get; init; }
    public int FailureCount { get; init; }
    public List<string> Errors { get; init; } = new();
    public TimeSpan ProcessingTime { get; init; }
}

public class BulkCreateDeviceDataCommandHandler : IRequestHandler<BulkCreateDeviceDataCommand, BulkCreateDeviceDataResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<BulkCreateDeviceDataCommandHandler> _logger;

    public BulkCreateDeviceDataCommandHandler(
        IApplicationDbContext context,
        ILogger<BulkCreateDeviceDataCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<BulkCreateDeviceDataResult> Handle(BulkCreateDeviceDataCommand request, CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var errors = new List<string>();
        var successCount = 0;
        var failureCount = 0;

        try
        {
            // Validate all device IDs exist in one query
            var deviceIds = request.DeviceDataItems.Select(x => x.DeviceId).Distinct().ToList();
            var existingDeviceIds = await _context.Devices
                .Where(d => deviceIds.Contains(d.Id))
                .Select(d => d.Id)
                .ToListAsync(cancellationToken);

            var validDeviceIds = existingDeviceIds.ToHashSet();

            // Prepare entities for bulk insert
            var entities = new List<Hems_Backend.Domain.Entities.DeviceData>();
            var batchSize = 1000; // Process in batches

            for (int i = 0; i < request.DeviceDataItems.Count; i += batchSize)
            {
                var batch = request.DeviceDataItems.Skip(i).Take(batchSize);
                
                foreach (var item in batch)
                {
                    try
                    {
                        // Validate device exists
                        if (!validDeviceIds.Contains(item.DeviceId))
                        {
                            errors.Add($"Device {item.DeviceId} not found");
                            failureCount++;
                            continue;
                        }

                        // Validate data size
                        var jsonString = item.Data.RootElement.GetRawText();
                        if (jsonString.Length > 1024 * 1024) // 1MB limit
                        {
                            errors.Add($"Data for device {item.DeviceId} exceeds 1MB limit");
                            failureCount++;
                            continue;
                        }

                        // Validate request date
                        if (item.RequestDate > DateTime.UtcNow.AddMinutes(5))
                        {
                            errors.Add($"RequestDate for device {item.DeviceId} is too far in the future");
                            failureCount++;
                            continue;
                        }

                        var entity = new Hems_Backend.Domain.Entities.DeviceData
                        {
                            DataType = item.DataType,
                            DeviceId = item.DeviceId,
                            RequestDate = item.RequestDate,
                            Data = item.Data
                        };

                        entities.Add(entity);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing device data item for device {DeviceId}", item.DeviceId);
                        errors.Add($"Error processing device {item.DeviceId}: {ex.Message}");
                        failureCount++;
                    }
                }

                // Bulk insert batch
                if (entities.Count > 0)
                {
                    await _context.DeviceData.AddRangeAsync(entities, cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);
                    entities.Clear();
                }
            }

            stopwatch.Stop();

            _logger.LogInformation("Bulk insert completed. Success: {SuccessCount}, Failures: {FailureCount}, Time: {ElapsedMs}ms",
                successCount, failureCount, stopwatch.ElapsedMilliseconds);

            return new BulkCreateDeviceDataResult
            {
                SuccessCount = successCount,
                FailureCount = failureCount,
                Errors = errors,
                ProcessingTime = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk device data insert");
            throw;
        }
    }
}

