using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Services;
using Hems_Backend.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Application.Devices.Commands.RegisterDevice;

public record DeviceLoginCommand : IRequest<DeviceLoginResult>
{
    public string SerialNumber { get; init; } = string.Empty;
    public string? QRCode { get; init; }
}

public record DeviceLoginResult
{
    public bool IsSuccess { get; init; }
    public Guid? DeviceId { get; init; }
    public string? DeviceName { get; init; }
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public string? ErrorMessage { get; init; }
}

public class DeviceLoginCommandHandler : IRequestHandler<DeviceLoginCommand, DeviceLoginResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IDeviceJwtService _deviceJwtService;
    private readonly ILogger<DeviceLoginCommandHandler> _logger;

    public DeviceLoginCommandHandler(
        IApplicationDbContext context,
        IDeviceJwtService deviceJwtService,
        ILogger<DeviceLoginCommandHandler> logger)
    {
        _context = context;
        _deviceJwtService = deviceJwtService;
        _logger = logger;
    }

    public async Task<DeviceLoginResult> Handle(DeviceLoginCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Device'ı SerialNumber veya QRCode ile bul
            var device = await _context.Devices
                .FirstOrDefaultAsync(d => 
                    d.SerialNumber == request.SerialNumber || 
                    d.QRCode == request.QRCode, 
                    cancellationToken);

            if (device == null)
            {
                _logger.LogWarning("Device not found with SerialNumber: {SerialNumber} or QRCode: {QRCode}", 
                    request.SerialNumber, request.QRCode);
                
                return new DeviceLoginResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Device not found"
                };
            }

            if (device.IsDeleted)
            {
                _logger.LogWarning("Deleted device attempted login: {ThingsBoardDeviceId}", device.Id);
                
                return new DeviceLoginResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Device is no longer active"
                };
            }

            // JWT Token oluştur
            var deviceName = $"{device.Manufacturer} {device.ModelNumber}";
            var jwtResult = await _deviceJwtService.GenerateTokenAsync(device.Id, deviceName);

            _logger.LogInformation("Device logged in successfully: {ThingsBoardDeviceId} ({DeviceName})", 
                device.Id, deviceName);

            return new DeviceLoginResult
            {
                IsSuccess = true,
                DeviceId = device.Id,
                DeviceName = deviceName,
                AccessToken = jwtResult.AccessToken,
                RefreshToken = jwtResult.RefreshToken,
                ExpiresAt = jwtResult.ExpiresAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during device login for SerialNumber: {SerialNumber}", request.SerialNumber);
            
            return new DeviceLoginResult
            {
                IsSuccess = false,
                ErrorMessage = "Login error occurred"
            };
        }
    }
}
