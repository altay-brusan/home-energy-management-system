using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Entities;
using Hems_Backend.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Hems_Backend.Application.DeviceComponents.Commands.UpdateDeviceComponent;

public record UpdateDeviceComponentCommand : IRequest
{
    public Guid Id { get; init; }
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

public class UpdateDeviceComponentCommandHandler : IRequestHandler<UpdateDeviceComponentCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateDeviceComponentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdateDeviceComponentCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.DeviceComponents
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (entity == null)
        {
            throw new Hems_Backend.Application.Common.Exceptions.NotFoundException(nameof(DeviceComponent), request.Id);
        }

        entity.DeviceType = request.DeviceType;
        entity.Name = request.Name;
        entity.Manufacturer = request.Manufacturer;
        entity.ModelNumber = request.ModelNumber;
        entity.SerialNumber = request.SerialNumber;
        entity.Specifications = request.Specifications;
        entity.WarrantyPeriod = request.WarrantyPeriod;
        entity.WarrantyUnit = request.WarrantyUnit;
        entity.WarrantyStartDate = request.WarrantyStartDate;

        await _context.SaveChangesAsync(cancellationToken);
    }
}

