using Hems_Backend.Application.Common.Models;
using MediatR;

namespace Hems_Backend.Application.Dashboard.Queries.GetSoh;

public record GetSohQuery : IRequest<SohResponseDto>
{
    public Guid DeviceId { get; init; }
}
