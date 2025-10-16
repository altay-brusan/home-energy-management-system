namespace Hems_Backend.Application.Common.Models;

public record SOCResponseDto
{
    public string Range { get; init; } = string.Empty;
    public List<SOCDataPoint> Data { get; init; } = new();
    public SOCStatistics Statistics { get; init; } = null!;
}

public record SOCDataPoint
{
    public string Timestamp { get; init; } = string.Empty;
    public double Value { get; init; }
    public string Unit { get; init; } = string.Empty;
}

public record SOCStatistics
{
    public double Total { get; init; }
    public double Peak { get; init; }
    public double Average { get; init; }
    public string Unit { get; init; } = string.Empty;
}
