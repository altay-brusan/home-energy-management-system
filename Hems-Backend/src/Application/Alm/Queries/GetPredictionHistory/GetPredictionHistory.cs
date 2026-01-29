using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Alm.Queries.GetPredictionHistory;

public record GetPredictionHistoryQuery : IRequest<PredictionHistoryResult>
{
    public Guid DeviceId { get; init; }
    public Guid? JobId { get; init; }
}

public class PredictionHistoryResult
{
    public bool Success { get; set; }
    public Guid? JobId { get; set; }
    public Guid DeviceId { get; set; }
    public AlmModelType ModelType { get; set; }
    public AlmJobStatus Status { get; set; }
    public List<PredictionDataPoint> Predictions { get; set; } = new();
    public string? Error { get; set; }
}

public class PredictionDataPoint
{
    public DateTime PredictedTime { get; set; }
    public double PredictedValue { get; set; }
    public string FeatureName { get; set; } = string.Empty;
}

internal sealed class GetPredictionHistoryQueryHandler : IRequestHandler<GetPredictionHistoryQuery, PredictionHistoryResult>
{
    private readonly IApplicationDbContext _context;

    public GetPredictionHistoryQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PredictionHistoryResult> Handle(GetPredictionHistoryQuery request, CancellationToken cancellationToken)
    {
        // Get the job - either specific or most recent for device
        var jobQuery = _context.AlmPredictionJobs.AsQueryable();

        if (request.JobId.HasValue)
        {
            jobQuery = jobQuery.Where(j => j.Id == request.JobId.Value);
        }
        else
        {
            jobQuery = jobQuery
                .Where(j => j.DeviceId == request.DeviceId)
                .OrderByDescending(j => j.Created);
        }

        var job = await jobQuery.FirstOrDefaultAsync(cancellationToken);

        if (job == null)
        {
            return new PredictionHistoryResult
            {
                Success = false,
                DeviceId = request.DeviceId,
                Error = "No prediction job found"
            };
        }

        var predictions = await _context.AlmPredictionHistories
            .Where(p => p.AlmPredictionJobId == job.Id)
            .OrderBy(p => p.PredictedTime)
            .Select(p => new PredictionDataPoint
            {
                PredictedTime = p.PredictedTime,
                PredictedValue = p.PredictedValue,
                FeatureName = p.FeatureName
            })
            .ToListAsync(cancellationToken);

        return new PredictionHistoryResult
        {
            Success = true,
            JobId = job.Id,
            DeviceId = job.DeviceId,
            ModelType = job.ModelType,
            Status = job.Status,
            Predictions = predictions
        };
    }
}
