using System.Text.Json;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Entities;

namespace Hems_Backend.Application.DeviceData.Commands.CreateDeviceData;

public record CreateDeviceDataCommand : IRequest<int>
{
    public string DataType { get; init; } = string.Empty;
    public Guid DeviceId { get; init; }
    public DateTime DeviceLocalTime { get; init; }
    public JsonDocument Data { get; init; } = JsonDocument.Parse("{}");
}

public class CreateDeviceDataCommandHandler : IRequestHandler<CreateDeviceDataCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ITimezoneService _timezoneService;
    private readonly IDeviceMappingService _deviceMapping;

    public CreateDeviceDataCommandHandler(
        IApplicationDbContext context,
        ITimezoneService timezoneService,
        IDeviceMappingService deviceMapping)
    {
        _context = context;
        _timezoneService = timezoneService;
        _deviceMapping = deviceMapping;
    }

    public async Task<int> Handle(CreateDeviceDataCommand request, CancellationToken cancellationToken)
    {
        // Convert UTC DeviceLocalTime to device local time using cached timezone service
        var localDeviceId = await _deviceMapping.GetDeviceIdAsync(request.DeviceId, cancellationToken);
        if (localDeviceId == null)
        {
            throw new ArgumentException($"Device mapping not found for ThingsBoardDeviceId {request.DeviceId}");
        }
        
        var localDeviceLocalTime = await _timezoneService.ConvertUtcToDeviceTimeAsync(
            localDeviceId.Value, 
            request.DeviceLocalTime, 
            cancellationToken);

        var entity = new Hems_Backend.Domain.Entities.DeviceData
        {
            DataType = request.DataType,
            ThingsBoardDeviceId = request.DeviceId,
            DeviceLocalTime = localDeviceLocalTime, // Store in device local time
            Data = request.Data
        };

        _context.DeviceData.Add(entity);

        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
