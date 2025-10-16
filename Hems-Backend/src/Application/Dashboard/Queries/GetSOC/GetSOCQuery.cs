using Hems_Backend.Application.Common.Models;
using MediatR;

namespace Hems_Backend.Application.Dashboard.Queries.GetSOC;

public record GetSOCQuery : IRequest<SOCResponseDto>
{
    public Guid DeviceId { get; init; }
}
