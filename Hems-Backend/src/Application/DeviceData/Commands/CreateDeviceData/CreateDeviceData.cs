using System.Text.Json;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Entities;

namespace Hems_Backend.Application.DeviceData.Commands.CreateDeviceData;

public record CreateDeviceDataCommand : IRequest<int>
{
    public string DataType { get; init; } = string.Empty;
    public Guid DeviceId { get; init; }
    public DateTime RequestDate { get; init; }
    public JsonDocument Data { get; init; } = JsonDocument.Parse("{}");
}

public class CreateDeviceDataCommandHandler : IRequestHandler<CreateDeviceDataCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateDeviceDataCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreateDeviceDataCommand request, CancellationToken cancellationToken)
    {
        var entity = new Hems_Backend.Domain.Entities.DeviceData
        {
            DataType = request.DataType,
            DeviceId = request.DeviceId,
            RequestDate = request.RequestDate,
            Data = request.Data
        };

        _context.DeviceData.Add(entity);

        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
