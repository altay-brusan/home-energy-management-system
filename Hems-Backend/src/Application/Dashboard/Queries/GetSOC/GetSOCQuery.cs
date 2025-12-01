using Hems_Backend.Application.Common.Models;
using MediatR;

namespace Hems_Backend.Application.Dashboard.Queries.GetSoc;

public record GetSocQuery : IRequest<SocResponseDto>
{
    public Guid DeviceId { get; init; }
}
