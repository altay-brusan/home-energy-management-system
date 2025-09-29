using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Services;
using Hems_Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Application.Common.Services;

public interface IDeviceJwtService
{
    Task<DeviceJwtResult> GenerateTokenAsync(Guid deviceId, string deviceName);
    Task<DeviceJwtValidationResult> ValidateTokenAsync(string token);
    string GenerateRefreshToken();
}

public class DeviceJwtResult
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public Guid DeviceId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
}

public class DeviceJwtValidationResult
{
    public bool IsValid { get; set; }
    public Guid? DeviceId { get; set; }
    public string? DeviceName { get; set; }
    public string? ErrorMessage { get; set; }
}

public class DeviceJwtService : IDeviceJwtService
{
    private readonly IApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DeviceJwtService> _logger;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public DeviceJwtService(
        IApplicationDbContext context,
        IConfiguration configuration,
        ILogger<DeviceJwtService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    public async Task<DeviceJwtResult> GenerateTokenAsync(Guid deviceId, string deviceName)
    {
        try
        {
            // Device'ın aktif olduğunu kontrol et
            var device = await _context.Devices
                .FirstOrDefaultAsync(d => d.Id == deviceId && !d.IsDeleted);

            if (device == null)
            {
                throw new InvalidOperationException("Device not found or inactive");
            }

            // JWT Claims oluştur
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, deviceId.ToString()),
                new(ClaimTypes.Name, deviceName),
                new("deviceId", deviceId.ToString()),
                new("deviceName", deviceName),
                new("type", "device"),
                new("manufacturer", device.Manufacturer),
                new("serialNumber", device.SerialNumber ?? ""),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Iat, 
                    new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(), 
                    ClaimValueTypes.Integer64)
            };

            // JWT Secret Key
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration["Jwt:Key"] ?? "your-super-secret-key-that-is-at-least-32-characters-long"));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Token expiration (1 saat)
            var expiresAt = DateTime.UtcNow.AddHours(1);

            // JWT Token oluştur
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? "HEMS",
                audience: _configuration["Jwt:Audience"] ?? "HEMS-Devices",
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials
            );

            var accessToken = _tokenHandler.WriteToken(token);
            var refreshToken = GenerateRefreshToken();

            _logger.LogInformation("Generated JWT token for device: {DeviceId} ({DeviceName})", 
                deviceId, deviceName);

            return new DeviceJwtResult
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt,
                DeviceId = deviceId,
                DeviceName = deviceName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating JWT token for device: {DeviceId}", deviceId);
            throw;
        }
    }

    public async Task<DeviceJwtValidationResult> ValidateTokenAsync(string token)
    {
        try
        {
            if (string.IsNullOrEmpty(token))
            {
                return new DeviceJwtValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Token is required"
                };
            }

            // JWT Secret Key
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration["Jwt:Key"] ?? "your-super-secret-key-that-is-at-least-32-characters-long"));

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"] ?? "HEMS",
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"] ?? "HEMS-Devices",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = _tokenHandler.ValidateToken(token, tokenValidationParameters, out var validatedToken);

            // Device ID'yi al
            var deviceIdClaim = principal.FindFirst("deviceId")?.Value;
            if (!Guid.TryParse(deviceIdClaim, out var deviceId))
            {
                return new DeviceJwtValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Invalid device ID in token"
                };
            }

            // Device'ın hala aktif olduğunu kontrol et
            var device = await _context.Devices
                .FirstOrDefaultAsync(d => d.Id == deviceId && !d.IsDeleted);

            if (device == null)
            {
                return new DeviceJwtValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Device is no longer active"
                };
            }

            var deviceName = principal.FindFirst("deviceName")?.Value ?? "";

            return new DeviceJwtValidationResult
            {
                IsValid = true,
                DeviceId = deviceId,
                DeviceName = deviceName
            };
        }
        catch (SecurityTokenExpiredException)
        {
            return new DeviceJwtValidationResult
            {
                IsValid = false,
                ErrorMessage = "Token has expired"
            };
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning("JWT validation failed: {Error}", ex.Message);
            return new DeviceJwtValidationResult
            {
                IsValid = false,
                ErrorMessage = "Invalid token"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating JWT token");
            return new DeviceJwtValidationResult
            {
                IsValid = false,
                ErrorMessage = "Token validation error"
            };
        }
    }

    public string GenerateRefreshToken()
    {
        return Guid.NewGuid().ToString("N");
    }
}
