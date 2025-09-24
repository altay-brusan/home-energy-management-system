using Hems_Backend.Application.Common.Models;

namespace Hems_Backend.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<string?> GetUserNameAsync(string userId);

    Task<bool> IsInRoleAsync(string userId, string role);

    Task<bool> AuthorizeAsync(string userId, string policyName);

    Task<(Result Result, string UserId)> CreateUserAsync(string userName, string password);

    Task<Result> DeleteUserAsync(string userId);

    // Role Management Methods
    Task<Result> CreateRoleAsync(string roleName);
    Task<Result> DeleteRoleAsync(string roleName);
    Task<Result> AssignRoleToUserAsync(string userId, string roleName);
    Task<Result> RemoveRoleFromUserAsync(string userId, string roleName);
    Task<IList<string>> GetUserRolesAsync(string userId);
    Task<IList<string>> GetAllRolesAsync();
}
