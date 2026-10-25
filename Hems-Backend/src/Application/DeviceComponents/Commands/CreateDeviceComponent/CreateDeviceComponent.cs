using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Entities;
using Hems_Backend.Domain.Enums;
using MediatR;
using System.Text.Json;

namespace Hems_Backend.Application.DeviceComponents.Commands.CreateDeviceComponent;

public record CreateDeviceComponentCommand : IRequest<Guid>
{
    public Guid DeviceId { get; init; }
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

public class CreateDeviceComponentCommandHandler : IRequestHandler<CreateDeviceComponentCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly TimeProvider _timeProvider;

    public CreateDeviceComponentCommandHandler(IApplicationDbContext context, IUser user, TimeProvider timeProvider)
    {
        _context = context;
        _user = user;
        _timeProvider = timeProvider;
    }

    public async Task<Guid> Handle(CreateDeviceComponentCommand request, CancellationToken cancellationToken)
    {
        var entity = new DeviceComponent
        {
            DeviceId = request.DeviceId,
            DeviceType = request.DeviceType,
            Name = request.Name,
            Manufacturer = request.Manufacturer,
            ModelNumber = request.ModelNumber,
            SerialNumber = request.SerialNumber,
            Specifications = request.Specifications,
            WarrantyPeriod = request.WarrantyPeriod,
            WarrantyUnit = request.WarrantyUnit,
            WarrantyStartDate = request.WarrantyStartDate,
        };

        _context.DeviceComponents.Add(entity);

        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}

