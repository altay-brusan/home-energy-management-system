namespace Hems_Backend.Infrastructure.Services;

/// <summary>
/// Configuration settings for app links and deep linking.
/// </summary>
public class AppLinksSettings
{
    public string IosAppStoreUrl { get; set; } = string.Empty;
    public string AndroidPlayStoreUrl { get; set; } = string.Empty;

    /// <summary>
    /// URL for web dashboard redirect (desktop users).
    /// TODO: Make this environment-specific in the future:
    ///   - Dev: https://hems-app-dev.n2nenergy.com/dashboard (or similar)
    ///   - Stage: https://hems-app-stg.n2nenergy.com/dashboard (or similar)
    ///   - Prod: https://hems-app.n2nenergy.com/dashboard
    /// Currently using the same URL for all environments.
    /// </summary>
    public string WebDashboardUrl { get; set; } = string.Empty;

    public string IosBundleId { get; set; } = string.Empty;
    public string IosTeamId { get; set; } = string.Empty;
}
