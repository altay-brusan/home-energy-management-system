using Hems_Backend.Application.Common.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Infrastructure.Identity;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<bool> UserExistsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user != null;
    }

    public async Task<List<string>> GetUserIdsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var users = await _userManager.Users
            .Where(u => u.Email != null && u.Email.Contains(email))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);
        
        return users;
    }

    public async Task<List<string>> GetUserIdsByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var users = await _userManager.Users
            .Where(u => 
                (u.FirstName != null && u.FirstName.Contains(name)) ||
                (u.Surname != null && u.Surname.Contains(name)) ||
                (u.FirstName != null && u.Surname != null && (u.FirstName + " " + u.Surname).Contains(name)))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);
        
        return users;
    }
}
























