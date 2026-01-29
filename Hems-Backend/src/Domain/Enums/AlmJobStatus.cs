namespace Hems_Backend.Domain.Enums;

/// <summary>
/// Status of an ALM prediction job
/// </summary>
public enum AlmJobStatus
{
    /// <summary>
    /// Job is queued and waiting to be processed
    /// </summary>
    Queued = 1,

    /// <summary>
    /// Job is currently being processed by the worker
    /// </summary>
    Processing = 2,

    /// <summary>
    /// Job completed successfully
    /// </summary>
    Completed = 3,

    /// <summary>
    /// Job failed with an error
    /// </summary>
    Failed = 4,

    /// <summary>
    /// Job was cancelled
    /// </summary>
    Cancelled = 5
}
