using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Hems_Backend.Application.Devices.Queries.GetDevicesWithComponents;

public record GetDevicesWithComponentsQuery : IRequest<List<DeviceWithComponentsDto>>
{
    public string? Manufacturer { get; init; }
    public bool? IsDeleted { get; init; }
}

public class GetDevicesWithComponentsQueryHandler : IRequestHandler<GetDevicesWithComponentsQuery, List<DeviceWithComponentsDto>>
{
    private readonly IApplicationDbContext _context;

    public GetDevicesWithComponentsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<DeviceWithComponentsDto>> Handle(GetDevicesWithComponentsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Devices
            .Include(d => d.DeviceComponents)
            .AsQueryable();

        if (!string.IsNullOrEmpty(request.Manufacturer))
        {
            query = query.Where(x => x.Manufacturer.Contains(request.Manufacturer));
        }

        if (request.IsDeleted.HasValue)
        {
            query = query.Where(x => x.IsDeleted == request.IsDeleted.Value);
        }
        else
        {
            // Default: only show non-deleted devices
            query = query.Where(x => !x.IsDeleted);
        }

        var devices = await query
            .OrderBy(x => x.Manufacturer)
            .ThenBy(x => x.ModelNumber)
            .ToListAsync(cancellationToken);

        var result = devices.Select(device => new DeviceWithComponentsDto
        {
            Device = new DeviceDto
            {
                Id = device.Id,
                Manufacturer = device.Manufacturer,
                ModelNumber = device.ModelNumber,
                SerialNumber = device.SerialNumber,
                TypeNumber = device.TypeNumber,
                QRCode = device.QRCode,
                Specifications = device.Specifications,
                IsDeleted = device.IsDeleted,
                InstallationDate = device.InstallationDate,
                CalibrationDate = device.CalibrationDate,
                NextCheckoutDate = device.NextCheckoutDate,
                WarrantyPeriod = device.WarrantyPeriod,
                WarrantyUnit = device.WarrantyUnit,
                WarrantyStartDate = device.WarrantyStartDate,
                Created = device.Created.DateTime,
                CreatedBy = device.CreatedBy,
                LastModified = device.LastModified.DateTime,
                LastModifiedBy = device.LastModifiedBy
            },
            Components = device.DeviceComponents
                .Where(c => !c.IsDeleted)
                .Select(component => new DeviceComponentDto
                {
                    Id = component.Id,
                    DeviceId = component.DeviceId,
                    DeviceType = component.DeviceType,
                    Name = component.Name,
                    Manufacturer = component.Manufacturer,
                    ModelNumber = component.ModelNumber,
                    SerialNumber = component.SerialNumber,
                    Specifications = component.Specifications,
                    WarrantyPeriod = component.WarrantyPeriod,
                    WarrantyUnit = component.WarrantyUnit,
                    WarrantyStartDate = component.WarrantyStartDate,
                    IsDeleted = component.IsDeleted,
                    Created = component.Created.DateTime,
                    CreatedBy = component.CreatedBy,
                    LastModified = component.LastModified.DateTime,
                    LastModifiedBy = component.LastModifiedBy
                })
                .ToList()
        }).ToList();

        return result;
    }
}

public class DeviceWithComponentsDto
{
    public DeviceDto Device { get; set; } = null!;
    public List<DeviceComponentDto> Components { get; set; } = new();
}

public class DeviceComponentDto
{
    public Guid Id { get; set; }
    public Guid DeviceId { get; set; }
    public DeviceType DeviceType { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Manufacturer { get; set; }
    public string? ModelNumber { get; set; }
    public string? SerialNumber { get; set; }
    public JsonDocument Specifications { get; set; } = JsonDocument.Parse("{}");
    public int? WarrantyPeriod { get; set; }
    public WarrantyUnit? WarrantyUnit { get; set; }
    public DateTime? WarrantyStartDate { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
}

