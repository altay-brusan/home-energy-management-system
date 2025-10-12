namespace Hems_Backend.Application.Common.Interfaces;

public interface IInvitationEmailService
{
    Task SendInvitationEmailAsync(string email, string ownerName, string siteName, string token, CancellationToken cancellationToken = default);
}

