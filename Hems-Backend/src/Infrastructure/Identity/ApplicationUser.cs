using Microsoft.AspNetCore.Identity;

namespace Hems_Backend.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? Surname { get; set; }

    /// <summary>
    /// Indicates if this is a demo user for app store assessments.
    /// Demo users get simulated data instead of real ThingsBoard data.
    /// </summary>
    public bool IsDemo { get; set; } = false;
}
