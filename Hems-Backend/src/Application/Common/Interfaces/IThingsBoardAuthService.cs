namespace Hems_Backend.Application.Common.Interfaces;

/// <summary>
/// Service for authenticating with ThingsBoard and managing JWT tokens
/// </summary>
public interface IThingsBoardAuthService
{
    /// <summary>
    /// Gets a valid JWT token for ThingsBoard API calls.
    /// Returns cached token if still valid, otherwise authenticates and gets new token.
    /// </summary>
    Task<string> GetJwtAsync(CancellationToken ct = default);
}
