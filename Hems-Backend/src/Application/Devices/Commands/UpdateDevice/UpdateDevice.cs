using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Entities;
using Hems_Backend.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Hems_Backend.Application.Devices.Commands.UpdateDevice;

public record UpdateDeviceCommand : IRequest
{
    public Guid Id { get; init; }
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
}

public class UpdateDeviceCommandHandler : IRequestHandler<UpdateDeviceCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateDeviceCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdateDeviceCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Devices
            .FindAsync([request.Id], cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        entity.Manufacturer = request.Manufacturer;
        entity.ModelNumber = request.ModelNumber;
        entity.SerialNumber = request.SerialNumber;
        entity.TypeNumber = request.TypeNumber;
        entity.QRCode = request.QRCode;
        entity.Specifications = request.Specifications;
        entity.InstallationDate = request.InstallationDate;
        entity.CalibrationDate = request.CalibrationDate;
        entity.NextCheckoutDate = request.NextCheckoutDate;
        entity.WarrantyPeriod = request.WarrantyPeriod;
        entity.WarrantyUnit = request.WarrantyUnit;
        entity.WarrantyStartDate = request.WarrantyStartDate;

        await _context.SaveChangesAsync(cancellationToken);
    }
}







