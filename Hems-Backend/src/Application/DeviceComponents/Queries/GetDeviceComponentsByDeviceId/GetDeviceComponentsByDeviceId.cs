using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.DeviceComponents.Queries.GetDeviceComponentsByDeviceId;

public record GetDeviceComponentsByDeviceIdQuery : IRequest<List<DeviceComponentDto>>
{
    public Guid DeviceId { get; init; }
    public bool? IsDeleted { get; init; }
}

public class GetDeviceComponentsByDeviceIdQueryHandler : IRequestHandler<GetDeviceComponentsByDeviceIdQuery, List<DeviceComponentDto>>
{
    private readonly IApplicationDbContext _context;

    public GetDeviceComponentsByDeviceIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<DeviceComponentDto>> Handle(GetDeviceComponentsByDeviceIdQuery request, CancellationToken cancellationToken)
    {
        var query = _context.DeviceComponents
            .Where(x => x.DeviceId == request.DeviceId);

        if (request.IsDeleted.HasValue)
        {
            query = query.Where(x => x.IsDeleted == request.IsDeleted.Value);
        }
        else
        {
            // Default: only show non-deleted components
            query = query.Where(x => !x.IsDeleted);
        }

        return await query
            .OrderBy(x => x.DeviceType)
            .ThenBy(x => x.Name)
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
            .ToListAsync(cancellationToken);
    }
}

