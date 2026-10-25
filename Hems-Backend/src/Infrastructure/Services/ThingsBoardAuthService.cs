using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Hems_Backend.Application.Common.Interfaces;

namespace Hems_Backend.Infrastructure.Services;

/// <summary>
/// Implementation of ThingsBoard authentication service
/// Manages JWT token lifecycle with caching
/// </summary>
public class ThingsBoardAuthService : IThingsBoardAuthService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _username;
    private readonly string _password;

    private string? _cachedToken;
    private DateTimeOffset _expiresAt;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ThingsBoardAuthService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;

        _baseUrl = config["ThingsBoard:BaseUrl"]
                    ?? throw new InvalidOperationException("ThingsBoard:BaseUrl missing");
        _username = config["ThingsBoard:Username"]
                    ?? throw new InvalidOperationException("ThingsBoard:Username missing");
        _password = config["ThingsBoard:Password"]
                    ?? throw new InvalidOperationException("ThingsBoard:Password missing");
    }

    public async Task<string> GetJwtAsync(CancellationToken ct = default)
    {
        if (!string.IsNullOrEmpty(_cachedToken) &&
            _expiresAt > DateTimeOffset.UtcNow.AddMinutes(1))
        {
            // Still valid, reuse
            return _cachedToken!;
        }

        // Login to TB
        var loginUrl = $"{_baseUrl.TrimEnd('/')}/api/auth/login";

        var body = new
        {
            username = _username,
            password = _password
        };

        using var resp = await _httpClient.PostAsJsonAsync(loginUrl, body, ct);
        resp.EnsureSuccessStatusCode();

        var stream = await resp.Content.ReadAsStreamAsync(ct);
        var login = await JsonSerializer.DeserializeAsync<LoginResponse>(stream, _jsonOptions, ct)
                    ?? throw new InvalidOperationException("Cannot parse TB login response");

        _cachedToken = login.Token;

        // TB typically encodes exp in JWT; for simplicity you can set a lifetime,
        // or (better) decode the JWT and use its exp claim. For now, just 12h:
        _expiresAt = DateTimeOffset.UtcNow.AddHours(12);

        return _cachedToken!;
    }

    private sealed class LoginResponse
    {
        public string Token { get; init; } = "";
        public string RefreshToken { get; init; } = "";
    }
}
