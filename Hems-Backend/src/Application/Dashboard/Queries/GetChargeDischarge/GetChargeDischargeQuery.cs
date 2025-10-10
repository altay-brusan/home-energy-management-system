using MediatR;

namespace Hems_Backend.Application.Dashboard.Queries.GetChargeDischarge;

public record GetChargeDischargeQuery : IRequest<ChargeDischargeDto>
{
    public Guid DeviceId { get; init; }
}
