using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Mappings;
using Hems_Backend.Application.Common.Exceptions;
using Hems_Backend.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Setups.Commands.CreateSetup;

public record CreateSetupCommand : IRequest<Guid>
{
    public Guid SiteId { get; init; }
    public string Name { get; init; } = "Default Setup";
    public string? DeviceQRCode { get; init; } // QR code ile device bulma
    public string? DeviceSerialNumber { get; init; } // Serial number ile device bulma
}

public class CreateSetupCommandHandler : IRequestHandler<CreateSetupCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly TimeProvider _timeProvider;

    public CreateSetupCommandHandler(IApplicationDbContext context, IUser user, TimeProvider timeProvider)
    {
        _context = context;
        _user = user;
        _timeProvider = timeProvider;
    }

    public async Task<Guid> Handle(CreateSetupCommand request, CancellationToken cancellationToken)
    {
        var userId = _user.Id ?? throw new UnauthorizedAccessException("User not authenticated");

        // Site yetki kontrolü - Kullanıcı bu site'a sahip mi veya paylaşılan site'da mı?
        var site = await _context.Sites
            .FirstOrDefaultAsync(s => s.Id == request.SiteId && 
                                     !s.IsDeleted && 
                                     (s.UserId == userId || 
                                      _context.UserSites.Any(us => us.SiteId == s.Id && us.UserId == userId)), 
                                 cancellationToken);

        if (site == null)
        {
            throw new Hems_Backend.Application.Common.Exceptions.NotFoundException(nameof(Site), request.SiteId);
        }

        // Site'ın zaten bir Setup'ı var mı kontrol et
        var existingSetup = await _context.Setups
            .FirstOrDefaultAsync(s => s.SiteId == request.SiteId, cancellationToken);

        if (existingSetup != null)
        {
            throw new System.ComponentModel.DataAnnotations.ValidationException("Bu site'ın zaten bir setup'ı var. Bir site'ın sadece bir setup'ı olabilir.");
        }

        // Device bulma (eğer belirtilmişse)
        Device? device = null;
        if (!string.IsNullOrEmpty(request.DeviceQRCode) || !string.IsNullOrEmpty(request.DeviceSerialNumber))
        {
            // QR Code ile device bulma
            if (!string.IsNullOrEmpty(request.DeviceQRCode))
            {
                device = await _context.Devices
                    .FirstOrDefaultAsync(d => d.QRCode == request.DeviceQRCode && !d.IsDeleted, cancellationToken);
            }
            // Serial Number ile device bulma
            else if (!string.IsNullOrEmpty(request.DeviceSerialNumber))
            {
                device = await _context.Devices
                    .FirstOrDefaultAsync(d => d.SerialNumber == request.DeviceSerialNumber && !d.IsDeleted, cancellationToken);
            }

            // Device bulunamazsa hata ver
            if (device == null)
            {
                var deviceIdentifier = !string.IsNullOrEmpty(request.DeviceQRCode) 
                    ? $"QR Code: {request.DeviceQRCode}" 
                    : $"Serial Number: {request.DeviceSerialNumber}";
                
                throw new ArgumentException($"Device not found with {deviceIdentifier}");
            }
            else if (_context.SetupDevices.Any(f=>f.DeviceId==device.Id))
            {
                throw new System.ComponentModel.DataAnnotations.ValidationException("bu cihaz zaten kullanımda.");

            }


        }

        // Setup oluştur
        var setup = new Setup
        {
            SiteId = request.SiteId,
            Name = request.Name,
        };

        _context.Setups.Add(setup);

        // Device varsa SetupDevice oluştur
        if (device != null)
        {
            var setupDevice = new SetupDevice
            {
                SetupId = setup.Id,
                DeviceId = device.Id,
            };

            _context.SetupDevices.Add(setupDevice);
            var devicesettingSchemeValues = await _context.ParameterValueSettingSchemes.Where(f => f.ModelNumber == device.ModelNumber).ToListAsync();

            foreach (var item in devicesettingSchemeValues)
            {
                var newSetupSettingsValue = new SetupDeviceParameterValue
                {
                    ParameterId = item.ParameterId,
                    ParameterGroupId = item.ParameterGroupId,
                    Value = item.Value,
                    SortOrder = item.SortOrder,
                    ModelNumber = item.ModelNumber,
                    SetupDeviceId = setupDevice.Id,
                };
                _context.SetupDeviceParameterValues.Add(newSetupSettingsValue);
            }
        }
        //TODO: Setting Olayı

        // Tek seferde kaydet - EF Core otomatik transaction yapar
        await _context.SaveChangesAsync(cancellationToken);
        
        return setup.Id;
    }
}




