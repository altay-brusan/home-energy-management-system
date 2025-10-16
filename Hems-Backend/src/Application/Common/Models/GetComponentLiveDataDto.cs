namespace Hems_Backend.Application.Common.Models;

public record GetComponentLiveDataRequest
{
    public Guid DeviceId { get; init; }
    public Guid ComponentId { get; init; }
}

public record GetComponentLiveDataResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public object? Data { get; init; }
}
