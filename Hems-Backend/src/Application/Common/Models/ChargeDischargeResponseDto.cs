namespace Hems_Backend.Application.Common.Models;

public record ChargeDischargeResponseDto
{
    public string Range { get; init; } = string.Empty;
    public List<ChargeDischargeDataPoint> Data { get; init; } = new();
    public ChargeDischargeStatistics Statistics { get; init; } = null!;
}

public record ChargeDischargeDataPoint
{
    public string Timestamp { get; init; } = string.Empty;
    public double ChargeValue { get; init; }
    public double DischargeValue { get; init; }
    public string Unit { get; init; } = string.Empty;
}

public record ChargeDischargeStatistics
{
    public double AverageCharge { get; init; }
    public double PeakCharge { get; init; }
    public double CurrentCharge { get; init; }
    public double AverageDischarge { get; init; }
    public double PeakDischarge { get; init; }
    public double CurrentDischarge { get; init; }
    public string Unit { get; init; } = string.Empty;
}
