using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Domain.Entities;
using Hems_Backend.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Application.Alm.Queries.GetJobResult;

public record GetJobResultQuery : IRequest<JobResultResponse>
{
    public Guid JobId { get; init; }
    public AlmModelType? ModelType { get; init; }
}

public class JobResultResponse
{
    public bool Success { get; set; }
    public Guid JobId { get; set; }
    public Guid DeviceId { get; set; }
    public string? AlmJobId { get; set; }
    public string Status { get; set; } = string.Empty;
    public AlmModelType ModelType { get; set; }
    public JobForecastResult? Result { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

public class JobForecastResult
{
    public List<JobForecastPoint> Forecast { get; set; } = new();
    public JobForecastMetadata? Metadata { get; set; }
}

public class JobForecastPoint
{
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
    public int Step { get; set; }
}

public class JobForecastMetadata
{
    public string Feature { get; set; } = string.Empty;
    public int IntervalSeconds { get; set; }
    public int Horizon { get; set; }
    public string Unit { get; set; } = string.Empty;
}

internal sealed class GetJobResultQueryHandler : IRequestHandler<GetJobResultQuery, JobResultResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IAlmService _almService;
    private readonly ILogger<GetJobResultQueryHandler> _logger;

    public GetJobResultQueryHandler(
        IApplicationDbContext context,
        IAlmService almService,
        ILogger<GetJobResultQueryHandler> logger)
    {
        _context = context;
        _almService = almService;
        _logger = logger;
    }

    public async Task<JobResultResponse> Handle(GetJobResultQuery request, CancellationToken cancellationToken)
    {
        // 1. Fetch job from database
        var job = await _context.AlmPredictionJobs
            .FirstOrDefaultAsync(j => j.Id == request.JobId, cancellationToken);

        if (job == null)
        {
            return new JobResultResponse
            {
                Success = false,
                Error = $"Job not found: {request.JobId}"
            };
        }

        // 2. Use provided model type or fall back to job's stored model type
        var effectiveModelType = request.ModelType ?? job.ModelType;

        // 3. Get model configuration for the endpoint
        var modelConfig = AlmModelConfiguration.GetConfig(effectiveModelType);

        // 3. Build base response from database
        var response = new JobResultResponse
        {
            Success = true,
            JobId = job.Id,
            DeviceId = job.DeviceId,
            AlmJobId = job.AlmJobId,
            Status = job.Status.ToString().ToLowerInvariant(),
            ModelType = job.ModelType,
            CreatedAt = job.Created,
            CompletedAt = job.CompletedAt,
            Error = job.ErrorMessage
        };

        // 4. If job has an AlmJobId, fetch results from AI container
        if (!string.IsNullOrEmpty(job.AlmJobId))
        {
            var almResult = await _almService.GetJobResultAsync(
                modelConfig.ModuleEndpoint,
                job.AlmJobId,
                cancellationToken);

            if (almResult != null)
            {
                // Update status from AI container
                response.Status = almResult.Status;
                response.Error = almResult.Error;

                if (almResult.CompletedAt.HasValue)
                {
                    response.CompletedAt = almResult.CompletedAt;
                }

                // Map forecast result
                if (almResult.Result != null)
                {
                    response.Result = new JobForecastResult
                    {
                        Forecast = almResult.Result.Forecast.Select(f => new JobForecastPoint
                        {
                            Timestamp = f.Timestamp,
                            Value = f.Value,
                            Step = f.Step
                        }).ToList(),
                        Metadata = almResult.Result.Metadata != null
                            ? new JobForecastMetadata
                            {
                                Feature = almResult.Result.Metadata.Feature,
                                IntervalSeconds = almResult.Result.Metadata.IntervalSeconds,
                                Horizon = almResult.Result.Metadata.Horizon,
                                Unit = almResult.Result.Metadata.Unit
                            }
                            : null
                    };

                    // 5. Store forecast in AlmPredictionHistory if not already stored
                    await StoreForecastIfNeeded(job, almResult.Result, modelConfig, cancellationToken);
                }

                // 6. Update job status in database if changed
                await UpdateJobStatusIfNeeded(job, almResult, cancellationToken);
            }
        }

        return response;
    }

    private async Task StoreForecastIfNeeded(
        AlmPredictionJob job,
        AlmForecastResult forecastResult,
        AlmModelConfig modelConfig,
        CancellationToken cancellationToken)
    {
        // Check if predictions already exist for this job
        var existingPredictions = await _context.AlmPredictionHistories
            .AnyAsync(h => h.AlmPredictionJobId == job.Id, cancellationToken);

        if (existingPredictions)
        {
            _logger.LogDebug("Predictions already stored for job {JobId}", job.Id);
            return;
        }

        if (forecastResult.Forecast == null || forecastResult.Forecast.Count == 0)
        {
            _logger.LogDebug("No forecast data to store for job {JobId}", job.Id);
            return;
        }

        var featureName = forecastResult.Metadata?.Feature ?? modelConfig.FeatureName;

        var histories = forecastResult.Forecast.Select(f => new AlmPredictionHistory
        {
            AlmPredictionJobId = job.Id,
            DeviceId = job.DeviceId,
            ModelType = job.ModelType,
            PredictedTime = f.Timestamp,
            PredictedValue = f.Value,
            FeatureName = featureName
        }).ToList();

        _context.AlmPredictionHistories.AddRange(histories);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Stored {Count} predictions for job {JobId}, device {DeviceId}",
            histories.Count, job.Id, job.DeviceId);
    }

    private async Task UpdateJobStatusIfNeeded(
        AlmPredictionJob job,
        AlmJobResultResponse almResult,
        CancellationToken cancellationToken)
    {
        var newStatus = almResult.Status.ToLowerInvariant() switch
        {
            "completed" => AlmJobStatus.Completed,
            "failed" or "error" => AlmJobStatus.Failed,
            "processing" => AlmJobStatus.Processing,
            "queued" => AlmJobStatus.Queued,
            _ => job.Status
        };

        if (newStatus != job.Status)
        {
            job.Status = newStatus;

            if (newStatus == AlmJobStatus.Completed || newStatus == AlmJobStatus.Failed)
            {
                job.CompletedAt = almResult.CompletedAt ?? DateTimeOffset.UtcNow;
            }

            if (!string.IsNullOrEmpty(almResult.Error))
            {
                job.ErrorMessage = almResult.Error;
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Updated job {JobId} status to {Status}",
                job.Id, newStatus);
        }
    }
}
