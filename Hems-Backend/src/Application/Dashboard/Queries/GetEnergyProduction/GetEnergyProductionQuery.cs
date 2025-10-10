using MediatR;

namespace Hems_Backend.Application.Dashboard.Queries.GetEnergyProduction;

public record GetEnergyProductionQuery : IRequest<EnergyProductionDto>
{
    public Guid DeviceId { get; init; }
    public string Period { get; init; } = string.Empty; // daily, weekly, monthly, yearly
}
