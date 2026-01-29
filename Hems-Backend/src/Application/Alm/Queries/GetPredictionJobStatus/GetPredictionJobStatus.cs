using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Alm.Queries.GetPredictionJobStatus;

public record GetPredictionJobStatusQuery : IRequest<PredictionJobStatusResult>
{
    public Guid JobId { get; init; }
}

public class PredictionJobStatusResult
{
    public bool Success { get; set; }
    public Guid JobId { get; set; }
    public Guid DeviceId { get; set; }
    public string? AlmJobId { get; set; }
    public AlmModelType ModelType { get; set; }
    public AlmJobStatus Status { get; set; }
    public int DataPointCount { get; set; }
    public DateTimeOffset Created { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Error { get; set; }
}

internal sealed class GetPredictionJobStatusQueryHandler : IRequestHandler<GetPredictionJobStatusQuery, PredictionJobStatusResult>
{
    private readonly IApplicationDbContext _context;

    public GetPredictionJobStatusQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PredictionJobStatusResult> Handle(GetPredictionJobStatusQuery request, CancellationToken cancellationToken)
    {
        var job = await _context.AlmPredictionJobs
            .FirstOrDefaultAsync(j => j.Id == request.JobId, cancellationToken);

        if (job == null)
        {
            return new PredictionJobStatusResult { Success = false, Error = $"Job not found: {request.JobId}" };
        }

        return new PredictionJobStatusResult
        {
            Success = true,
            JobId = job.Id,
            DeviceId = job.DeviceId,
            AlmJobId = job.AlmJobId,
            ModelType = job.ModelType,
            Status = job.Status,
            DataPointCount = job.DataPointCount,
            Created = job.Created,
            CompletedAt = job.CompletedAt,
            ErrorMessage = job.ErrorMessage
        };
    }
}
