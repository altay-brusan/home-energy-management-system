using System.Text.Json;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Application.Alm.Commands.ProcessAlmCallback;

public record ProcessAlmCallbackCommand : IRequest<ProcessAlmCallbackResult>
{
    public JsonElement RawBody { get; init; }
}

public class ProcessAlmCallbackResult
{
    public bool Success { get; set; }
    public Guid? JobId { get; set; }
    public Guid? DeviceId { get; set; }
    public string? AlmJobId { get; set; }
    public AlmModelType? ModelType { get; set; }
    public string? Status { get; set; }
    public string? Error { get; set; }
}

internal sealed class ProcessAlmCallbackCommandHandler : IRequestHandler<ProcessAlmCallbackCommand, ProcessAlmCallbackResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<ProcessAlmCallbackCommandHandler> _logger;

    public ProcessAlmCallbackCommandHandler(
        IApplicationDbContext context,
        ILogger<ProcessAlmCallbackCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ProcessAlmCallbackResult> Handle(ProcessAlmCallbackCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var rawJson = request.RawBody.GetRawText();
            _logger.LogInformation("ALM callback received: {Body}", rawJson);

            // Extract job_id from the callback
            string? almJobId = null;
            if (request.RawBody.TryGetProperty("job_id", out var jobIdProp))
                almJobId = jobIdProp.GetString();

            if (string.IsNullOrEmpty(almJobId))
            {
                _logger.LogWarning("ALM callback missing job_id");
                return new ProcessAlmCallbackResult { Success = false, Error = "Missing job_id in callback" };
            }

            // Find the prediction job
            var job = await _context.AlmPredictionJobs
                .FirstOrDefaultAsync(j => j.AlmJobId == almJobId, cancellationToken);

            if (job == null)
            {
                _logger.LogWarning("ALM callback for unknown job: {AlmJobId}", almJobId);
                return new ProcessAlmCallbackResult { Success = false, Error = $"Unknown job: {almJobId}" };
            }

            // Extract status from callback
            string? status = null;
            if (request.RawBody.TryGetProperty("status", out var statusProp))
                status = statusProp.GetString();

            // Extract error message if present
            string? errorMsg = null;
            if (request.RawBody.TryGetProperty("error", out var errorProp))
                errorMsg = errorProp.GetString();

            // Update job status based on callback
            // Note: Forecast data is NOT parsed here - it's fetched via GET /api/Alm/job/{jobId}
            if (status == "completed")
            {
                job.Status = AlmJobStatus.Completed;
                job.CompletedAt = DateTimeOffset.UtcNow;

                _logger.LogInformation(
                    "ALM job {AlmJobId} completed. JobId: {JobId}, DeviceId: {DeviceId}",
                    almJobId, job.Id, job.DeviceId);
            }
            else if (status == "failed" || status == "error")
            {
                job.Status = AlmJobStatus.Failed;
                job.ErrorMessage = errorMsg ?? "ALM job failed";
                job.CompletedAt = DateTimeOffset.UtcNow;

                _logger.LogWarning(
                    "ALM job {AlmJobId} failed. JobId: {JobId}, DeviceId: {DeviceId}, Error: {Error}",
                    almJobId, job.Id, job.DeviceId, errorMsg);
            }
            else if (status == "processing")
            {
                job.Status = AlmJobStatus.Processing;

                _logger.LogInformation(
                    "ALM job {AlmJobId} processing. JobId: {JobId}, DeviceId: {DeviceId}",
                    almJobId, job.Id, job.DeviceId);
            }

            await _context.SaveChangesAsync(cancellationToken);

            return new ProcessAlmCallbackResult
            {
                Success = true,
                JobId = job.Id,
                DeviceId = job.DeviceId,
                AlmJobId = job.AlmJobId,
                ModelType = job.ModelType,
                Status = status,
                Error = errorMsg
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ALM callback");
            return new ProcessAlmCallbackResult { Success = false, Error = ex.Message };
        }
    }
}
