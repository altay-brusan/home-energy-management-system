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

    /// <summary>
    /// Indicates if this user account has been soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// The date and time when the user account was soft-deleted.
    /// </summary>
    public DateTimeOffset? DeletedAt { get; set; }
}
