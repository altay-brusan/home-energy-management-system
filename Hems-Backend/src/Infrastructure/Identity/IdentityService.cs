using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IUserClaimsPrincipalFactory<ApplicationUser> _userClaimsPrincipalFactory;
    private readonly IAuthorizationService _authorizationService;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IUserClaimsPrincipalFactory<ApplicationUser> userClaimsPrincipalFactory,
        IAuthorizationService authorizationService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _userClaimsPrincipalFactory = userClaimsPrincipalFactory;
        _authorizationService = authorizationService;
    }

    public async Task<string?> GetUserNameAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        return user?.UserName;
    }

    public async Task<(Result Result, string UserId)> CreateUserAsync(string userName, string password)
    {
        var user = new ApplicationUser
        {
            UserName = userName,
            Email = userName,
        };

        var result = await _userManager.CreateAsync(user, password);

        return (result.ToApplicationResult(), user.Id);
    }

    public async Task<bool> IsInRoleAsync(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);

        return user != null && await _userManager.IsInRoleAsync(user, role);
    }

    public async Task<bool> AuthorizeAsync(string userId, string policyName)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return false;
        }

        var principal = await _userClaimsPrincipalFactory.CreateAsync(user);

        var result = await _authorizationService.AuthorizeAsync(principal, policyName);

        return result.Succeeded;
    }

    public async Task<Result> DeleteUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        return user != null ? await DeleteUserAsync(user) : Result.Success();
    }

    public async Task<Result> DeleteUserAsync(ApplicationUser user)
    {
        var result = await _userManager.DeleteAsync(user);

        return result.ToApplicationResult();
    }

    // Role Management Methods
    public async Task<Result> CreateRoleAsync(string roleName)
    {
        var role = new IdentityRole(roleName);
        var result = await _roleManager.CreateAsync(role);
        return result.ToApplicationResult();
    }

    public async Task<Result> DeleteRoleAsync(string roleName)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null)
        {
            return Result.Success();
        }

        var result = await _roleManager.DeleteAsync(role);
        return result.ToApplicationResult();
    }

    public async Task<Result> AssignRoleToUserAsync(string userId, string roleName)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Result.Failure(new[] { "User not found." });
        }

        var result = await _userManager.AddToRoleAsync(user, roleName);
        return result.ToApplicationResult();
    }

    public async Task<Result> RemoveRoleFromUserAsync(string userId, string roleName)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Result.Failure(new[] { "User not found." });
        }

        var result = await _userManager.RemoveFromRoleAsync(user, roleName);
        return result.ToApplicationResult();
    }

    public async Task<IList<string>> GetUserRolesAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return new List<string>();
        }

        return await _userManager.GetRolesAsync(user);
    }

    public async Task<IList<string>> GetAllRolesAsync()
    {
        var roles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
        return roles;
    }

    public async Task<string?> GetUserIdByEmailAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user?.Id;
    }

    public async Task<string?> GetUserEmailAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user?.Email;
    }

    public async Task<Result> UpdateUserAsync(string userId, string? firstName, string? surname, string? email, string? phoneNumber, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Result.Failure(new[] { "User not found." });
        }

        // Update properties only if they are provided
        if (firstName != null)
            user.FirstName = firstName;

        if (surname != null)
            user.Surname = surname;

        if (email != null)
            user.Email = email;

        if (phoneNumber != null)
            user.PhoneNumber = phoneNumber;

        var result = await _userManager.UpdateAsync(user);
        return result.ToApplicationResult();
    }

    public async Task<Result> SoftDeleteUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Result.Failure(new[] { "User not found." });
        }

        user.IsDeleted = true;
        user.DeletedAt = DateTimeOffset.UtcNow;

        // Anonymize PII for GDPR compliance (Right to Erasure)
        // Keep the user record for audit trail FK references (CreatedBy/LastModifiedBy store user IDs)
        user.FirstName = null;
        user.Surname = null;
        user.Email = $"deleted-{userId}@deleted.local";
        user.NormalizedEmail = $"DELETED-{userId}@DELETED.LOCAL";
        user.UserName = $"deleted-{userId}";
        user.NormalizedUserName = $"DELETED-{userId}";
        user.PhoneNumber = null;
        user.PasswordHash = null;

        // Invalidate all existing tokens by updating the security stamp
        await _userManager.UpdateSecurityStampAsync(user);

        var result = await _userManager.UpdateAsync(user);
        return result.ToApplicationResult();
    }

    public async Task<bool> IsUserDemoAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user?.IsDemo ?? false;
    }
}
