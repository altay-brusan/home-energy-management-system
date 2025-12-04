using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Entities;
using Hems_Backend.Domain.Enums;
using MediatR;
using System.Text.Json;

namespace Hems_Backend.Application.Devices.Commands.CreateDevice;

public record CreateDeviceCommand : IRequest<Guid>
{
    public string Manufacturer { get; init; } = string.Empty;
    public string? ModelNumber { get; init; }
    public string? SerialNumber { get; init; }
    public string? TypeNumber { get; init; }
    public string? QRCode { get; init; }
    public JsonDocument Specifications { get; init; } = JsonDocument.Parse("{}");
    public DateTime? InstallationDate { get; init; }
    public DateTime? CalibrationDate { get; init; }
    public DateTime? NextCheckoutDate { get; init; }
    public int? WarrantyPeriod { get; init; }
    public WarrantyUnit? WarrantyUnit { get; init; }
    public DateTime? WarrantyStartDate { get; init; }
    public List<CreateDeviceComponentCommand> DeviceComponents { get; init; } = new();
}

public record CreateDeviceComponentCommand
{
    public DeviceType DeviceType { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Manufacturer { get; init; }
    public string? ModelNumber { get; init; }
    public string? SerialNumber { get; init; }
    public JsonDocument Specifications { get; init; } = JsonDocument.Parse("{}");
    public int? WarrantyPeriod { get; init; }
    public WarrantyUnit? WarrantyUnit { get; init; }
    public DateTime? WarrantyStartDate { get; init; }
}

public class CreateDeviceCommandHandler : IRequestHandler<CreateDeviceCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly TimeProvider _timeProvider;
    private readonly IThingsBoardConnectorService _tbConnector;
    private readonly IDeviceHeartbeatCache _heartbeatCache;

    public CreateDeviceCommandHandler(
        IApplicationDbContext context, 
        IUser user, 
        TimeProvider timeProvider,
        IThingsBoardConnectorService tbConnector,
        IDeviceHeartbeatCache heartbeatCache)
    {
        _context = context;
        _user = user;
        _timeProvider = timeProvider;
        _tbConnector = tbConnector;
        _heartbeatCache = heartbeatCache;
    }

    public async Task<Guid> Handle(CreateDeviceCommand request, CancellationToken cancellationToken)
    {
        var utcNow = _timeProvider.GetUtcNow();
        
        // Step 1: Create device in ThingsBoard first
        var tbDeviceRequest = new TbDeviceCreateRequest
        {
            Name = request.SerialNumber ?? request.QRCode ?? Guid.NewGuid().ToString(),
            Label = $"{request.Manufacturer} {request.ModelNumber}".Trim(),
            DeviceProfileName = "default",
            ServerAttributes = new Dictionary<string, object>
            {
                ["Manufacturer"] = request.Manufacturer,
                ["ModelNumber"] = request.ModelNumber ?? string.Empty,
                ["SerialNumber"] = request.SerialNumber ?? string.Empty,
                ["TypeNumber"] = request.TypeNumber ?? string.Empty,
                ["QRCode"] = request.QRCode ?? string.Empty
            }
        };

        var tbResult = await _tbConnector.CreateDeviceAsync(tbDeviceRequest, cancellationToken);
        
        // Step 2: Create device in local database with TB device ID
        var entity = new Device
        {
            ThingsBoardDeviceId = tbResult.DeviceId,
            Name = tbResult.Name,
            Manufacturer = request.Manufacturer,
            ModelNumber = request.ModelNumber,
            SerialNumber = request.SerialNumber,
            TypeNumber = request.TypeNumber,
            QRCode = request.QRCode,
            Specifications = request.Specifications,
            InstallationDate = request.InstallationDate,
            CalibrationDate = request.CalibrationDate,
            NextCheckoutDate = request.NextCheckoutDate,
            WarrantyPeriod = request.WarrantyPeriod,
            WarrantyUnit = request.WarrantyUnit,
            WarrantyStartDate = request.WarrantyStartDate,
            CreatedBy = _user.Id,
            Created = utcNow,
            LastModifiedBy = _user.Id,
            LastModified = utcNow
        };

        _context.Devices.Add(entity);

        // DeviceComponent'leri ekle
        foreach (var componentCommand in request.DeviceComponents)
        {
            var component = new DeviceComponent
            {
                DeviceId = entity.Id, // Bu entity.Id'yi SaveChangesAsync'den sonra alacağız
                DeviceType = componentCommand.DeviceType,
                Name = componentCommand.Name,
                Manufacturer = componentCommand.Manufacturer,
                ModelNumber = componentCommand.ModelNumber,
                SerialNumber = componentCommand.SerialNumber,
                Specifications = componentCommand.Specifications,
                WarrantyPeriod = componentCommand.WarrantyPeriod,
                WarrantyUnit = componentCommand.WarrantyUnit,
                WarrantyStartDate = componentCommand.WarrantyStartDate,
                CreatedBy = _user.Id,
                Created = utcNow,
                LastModifiedBy = _user.Id,
                LastModified = utcNow
            };
            
            entity.DeviceComponents.Add(component);
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Add the newly created device to the heartbeat cache
        await _heartbeatCache.AddDeviceToCacheAsync(entity.ThingsBoardDeviceId, cancellationToken);

        return entity.Id;
    }
}



