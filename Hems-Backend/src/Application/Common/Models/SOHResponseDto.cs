namespace Hems_Backend.Application.Common.Models;

public record SohResponseDto
{
    public string Range { get; init; } = string.Empty;
    public List<SohDataPoint> Data { get; init; } = new();
    public SohStatistics Statistics { get; init; } = null!;
}

public record SohDataPoint
{
    public string Timestamp { get; init; } = string.Empty;
    public double Value { get; init; }
    public string Unit { get; init; } = string.Empty;
}

public record SohStatistics
{
    public double Peak { get; init; }
    public double Average { get; init; }
    public string Unit { get; init; } = string.Empty;
}
