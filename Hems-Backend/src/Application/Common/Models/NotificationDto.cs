namespace Hems_Backend.Application.Common.Models;

public class NotificationDto
{
    public Guid Id { get; init; }
    public string Message { get; init; } = string.Empty;
    public bool IsRead { get; init; }
    public Guid InvitationId { get; init; }
    public DateTimeOffset Created { get; init; }
}

