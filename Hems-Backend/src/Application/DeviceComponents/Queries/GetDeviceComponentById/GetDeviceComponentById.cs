using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.DeviceComponents.Queries.GetDeviceComponentById;

public record GetDeviceComponentByIdQuery : IRequest<DeviceComponentDto?>
{
    public Guid Id { get; init; }
}

public class GetDeviceComponentByIdQueryHandler : IRequestHandler<GetDeviceComponentByIdQuery, DeviceComponentDto?>
{
    private readonly IApplicationDbContext _context;

    public GetDeviceComponentByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DeviceComponentDto?> Handle(GetDeviceComponentByIdQuery request, CancellationToken cancellationToken)
    {
        return await _context.DeviceComponents
            .Where(x => x.Id == request.Id && !x.IsDeleted)
            .Select(x => new DeviceComponentDto
            {
                Id = x.Id,
                DeviceId = x.DeviceId,
                DeviceType = x.DeviceType,
                Name = x.Name,
                Manufacturer = x.Manufacturer,
                ModelNumber = x.ModelNumber,
                SerialNumber = x.SerialNumber,
                Specifications = x.Specifications,
                WarrantyPeriod = x.WarrantyPeriod,
                WarrantyUnit = x.WarrantyUnit,
                WarrantyStartDate = x.WarrantyStartDate,
                IsDeleted = x.IsDeleted,
                Created = x.Created.DateTime,
                CreatedBy = x.CreatedBy,
                LastModified = x.LastModified.DateTime,
                LastModifiedBy = x.LastModifiedBy
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}

