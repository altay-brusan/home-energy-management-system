namespace Hems_Backend.Application.Common.Interfaces;

public interface IUserService
{
    Task<bool> UserExistsAsync(string userId, CancellationToken cancellationToken = default);
}





















