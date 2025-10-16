using Hems_Backend.Application.Common.Models;
using MediatR;

namespace Hems_Backend.Application.Dashboard.Queries.GetHero;

public record GetHeroQuery : IRequest<HeroResponseDto>
{
    public Guid DeviceId { get; init; }
}
