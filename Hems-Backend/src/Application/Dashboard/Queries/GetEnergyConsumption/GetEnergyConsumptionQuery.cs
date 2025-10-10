using MediatR;

namespace Hems_Backend.Application.Dashboard.Queries.GetEnergyConsumption;

public record GetEnergyConsumptionQuery : IRequest<EnergyConsumptionDto>
{
    public Guid DeviceId { get; init; }
    public string Period { get; init; } = string.Empty; // daily, weekly, monthly, yearly
}
