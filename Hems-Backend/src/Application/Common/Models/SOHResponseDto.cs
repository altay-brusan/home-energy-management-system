namespace Hems_Backend.Application.Common.Models;

public record SOHResponseDto
{
    public string Range { get; init; } = string.Empty;
    public List<SOHDataPoint> Data { get; init; } = new();
    public SOHStatistics Statistics { get; init; } = null!;
}

public record SOHDataPoint
{
    public string Timestamp { get; init; } = string.Empty;
    public double Value { get; init; }
    public string Unit { get; init; } = string.Empty;
}

public record SOHStatistics
{
    public double Total { get; init; }
    public double Peak { get; init; }
    public double Average { get; init; }
    public string Unit { get; init; } = string.Empty;
}
