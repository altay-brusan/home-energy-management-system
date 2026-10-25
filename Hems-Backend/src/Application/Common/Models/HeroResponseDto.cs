namespace Hems_Backend.Application.Common.Models;

public record HeroResponseDto
{
    public string Title { get; init; } = string.Empty;
    public string Subtitle { get; init; } = string.Empty;
    public HeroMetrics Metrics { get; init; } = null!;
}

public record HeroMetrics
{
    public double TodayEnergy { get; init; }
    public double CurrentPower { get; init; }
    public string LastSync { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
}
