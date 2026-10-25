namespace Hems_Backend.Application.PowerFlow.Common.Models;

public class PowerFlowDto
{
    public DateTimeOffset TimestampUtc { get; init; }

    public Guid DeviceId { get; init; }

    // Power values in kW (from your PowerFlowState)
    public double SolarPowerKw { get; init; }       // pv.totalPowerW / 1000
    public double GridPowerKw { get; init; }        // grid.totalPowerW / 1000 (+import, -export)
    public double LoadPowerKw { get; init; }        // load.totalPowerW / 1000
    public double BatteryPowerKw { get; init; }     // battery.powerW / 1000 (+discharge, -charge)
    public double GeneratorPowerKw { get; init; }   // generator.totalPowerW / 1000

    // Optional: raw JSON if you want to debug on FE
    public string? RawJson { get; init; }
}
