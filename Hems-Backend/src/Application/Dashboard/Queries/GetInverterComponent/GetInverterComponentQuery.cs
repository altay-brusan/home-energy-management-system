using MediatR;

namespace Hems_Backend.Application.Dashboard.Queries.GetInverterComponent;

public record GetInverterComponentQuery : IRequest<InverterComponentDto>
{
    public Guid DeviceId { get; init; }
}
