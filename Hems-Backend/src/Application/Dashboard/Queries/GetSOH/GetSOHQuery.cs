using Hems_Backend.Application.Common.Models;
using MediatR;

namespace Hems_Backend.Application.Dashboard.Queries.GetSOH;

public record GetSOHQuery : IRequest<SOHResponseDto>
{
    public Guid DeviceId { get; init; }
}
