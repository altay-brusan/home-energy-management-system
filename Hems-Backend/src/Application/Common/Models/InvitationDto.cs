using Hems_Backend.Domain.Enums;

namespace Hems_Backend.Application.Common.Models;

public class InvitationDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string? InvitedUserId { get; init; }
    public string InvitedByUserId { get; init; } = string.Empty;
    public string InvitedByUserName { get; init; } = string.Empty;
    public Guid SiteId { get; init; }
    public string SiteName { get; init; } = string.Empty;
    public string Token { get; init; } = string.Empty;
    public InvitationStatus Status { get; init; }
    public DateTimeOffset? AcceptanceTime { get; init; }
    public DateTimeOffset Created { get; init; }
}

