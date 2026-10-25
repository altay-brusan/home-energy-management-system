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
    
    // Minimal user lookup methods for invitations
    Task<string?> GetUserIdByEmailAsync(string email);
    Task<string?> GetUserEmailAsync(string userId);
    
    // User update methods
    Task<Result> UpdateUserAsync(string userId, string? firstName, string? surname, string? email, string? phoneNumber, CancellationToken cancellationToken = default);

    // Soft delete methods
    Task<Result> SoftDeleteUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> IsUserDemoAsync(string userId, CancellationToken cancellationToken = default);
}
