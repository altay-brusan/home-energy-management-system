using Hems_Backend.Application.Common.Models;
using MediatR;

namespace Hems_Backend.Application.Dashboard.Queries.GetSocHistory;

public record GetSocHistoryQuery : IRequest<SocHistoryResponseDto>
{
    public Guid DeviceId { get; init; }
    public DateOnly Date { get; init; }
}
