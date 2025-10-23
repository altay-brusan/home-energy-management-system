namespace Hems_Backend.Application.Common.Interfaces;

public interface IUserService
{
    Task<List<string>> GetUserIdsByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<List<string>> GetUserIdsByNameAsync(string name, CancellationToken cancellationToken = default);
}