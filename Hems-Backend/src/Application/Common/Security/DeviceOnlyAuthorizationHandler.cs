using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Application.Common.Security;

public class DeviceOnlyRequirement : IAuthorizationRequirement
{
}

public class DeviceOnlyAuthorizationHandler : AuthorizationHandler<DeviceOnlyRequirement>
{
    private readonly IDeviceJwtService _deviceJwtService;
    private readonly ILogger<DeviceOnlyAuthorizationHandler> _logger;

    public DeviceOnlyAuthorizationHandler(
        IDeviceJwtService deviceJwtService,
        ILogger<DeviceOnlyAuthorizationHandler> logger)
    {
        _deviceJwtService = deviceJwtService;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DeviceOnlyRequirement requirement)
    {
        try
        {
            // HTTP Context'i al
            var httpContext = context.Resource as HttpContext;
            if (httpContext == null)
            {
                _logger.LogWarning("HTTP context not available for device authorization");
                context.Fail();
                return;
            }

            // Authorization header'ından Bearer token'ı al
            var authHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();
            string? token = null;

            if (authHeader?.StartsWith("Bearer ") == true)
            {
                token = authHeader.Substring("Bearer ".Length).Trim();
            }

            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("No Bearer token found in authorization header");
                context.Fail();
                return;
            }

            // JWT Token'ı validate et
            var validationResult = await _deviceJwtService.ValidateTokenAsync(token);

            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Device JWT validation failed: {Error}", validationResult.ErrorMessage);
                context.Fail();
                return;
            }

            // Device bilgilerini context'e ekle
            httpContext.Items["DeviceId"] = validationResult.DeviceId;
            httpContext.Items["DeviceName"] = validationResult.DeviceName;
            httpContext.Items["IsDeviceRequest"] = true;

            // Device claims'leri ekle
            var deviceClaims = new List<System.Security.Claims.Claim>
            {
                new("deviceId", validationResult.DeviceId.ToString()!),
                new("deviceName", validationResult.DeviceName ?? ""),
                new("type", "device")
            };

            var deviceIdentity = new System.Security.Claims.ClaimsIdentity(deviceClaims, "Device");
            context.User.AddIdentity(deviceIdentity);

            _logger.LogDebug("Device authorization successful: {DeviceId} ({DeviceName})", 
                validationResult.DeviceId, validationResult.DeviceName);

            context.Succeed(requirement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during device authorization");
            context.Fail();
        }
    }
}
