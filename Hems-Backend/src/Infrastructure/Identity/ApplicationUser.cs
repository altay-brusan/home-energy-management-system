using Microsoft.AspNetCore.Identity;

namespace Hems_Backend.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? Surname { get; set; }
}
