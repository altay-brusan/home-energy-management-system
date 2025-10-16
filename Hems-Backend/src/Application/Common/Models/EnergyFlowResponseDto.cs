namespace Hems_Backend.Application.Common.Models;

public record EnergyFlowResponseDto
{
    public string Date { get; init; } = string.Empty;
    public string Range { get; init; } = string.Empty;
    public List<EnergySource> Sources { get; init; } = new();
    public List<EnergyDestination> Destinations { get; init; } = new();
    public List<EnergyFlow> Flows { get; init; } = new();
}

public record EnergySource
{
    public string Id { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public double Value { get; init; }
    public double Percentage { get; init; }
    public string Color { get; init; } = string.Empty;
}

public record EnergyDestination
{
    public string Id { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public double Value { get; init; }
    public double Percentage { get; init; }
    public string Color { get; init; } = string.Empty;
}

public record EnergyFlow
{
    public string Source { get; init; } = string.Empty;
    public string Target { get; init; } = string.Empty;
    public double Value { get; init; }
}
