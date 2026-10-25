//using Hems_Backend.Application.Common.Interfaces;
//using Hems_Backend.Application.Common.Models;
//using MediatR;
//using Microsoft.EntityFrameworkCore;

//namespace Hems_Backend.Application.Devices.Queries.GetDevices;

//public record GetDevicesQuery : IRequest<PaginatedList<DeviceDto>>
//{
//    public string? Manufacturer { get; init; }
//    public bool? IsDeleted { get; init; }
//    public int PageNumber { get; init; } = 1;
//    public int PageSize { get; init; } = 10;
//}

//public class GetDevicesQueryHandler : IRequestHandler<GetDevicesQuery, PaginatedList<DeviceDto>>
//{
//    private readonly IApplicationDbContext _context;

//    public GetDevicesQueryHandler(IApplicationDbContext context)
//    {
//        _context = context;
//    }

//    public async Task<PaginatedList<DeviceDto>> Handle(GetDevicesQuery request, CancellationToken cancellationToken)
//    {
//        var query = _context.Devices.AsQueryable();

//        if (!string.IsNullOrEmpty(request.Manufacturer))
//        {
//            query = query.Where(x => x.Manufacturer.Contains(request.Manufacturer));
//        }

//        if (request.IsDeleted.HasValue)
//        {
//            query = query.Where(x => x.IsDeleted == request.IsDeleted.Value);
//        }
//        else
//        {
//            // Default: only show non-deleted devices
//            query = query.Where(x => !x.IsDeleted);
//        }

//        var devices = await query
//            .OrderBy(x => x.Manufacturer)
//            .ThenBy(x => x.ModelNumber)
//            .Select(x => new DeviceDto
//            {
//                Id = x.Id,
//                Manufacturer = x.Manufacturer,
//                ModelNumber = x.ModelNumber,
//                SerialNumber = x.SerialNumber,
//                TypeNumber = x.TypeNumber,
//                QRCode = x.QRCode,
//                Specifications = x.Specifications,
//                IsDeleted = x.IsDeleted,
//                InstallationDate = x.InstallationDate,
//                CalibrationDate = x.CalibrationDate,
//                NextCheckoutDate = x.NextCheckoutDate,
//                WarrantyPeriod = x.WarrantyPeriod,
//                WarrantyUnit = x.WarrantyUnit,
//                WarrantyStartDate = x.WarrantyStartDate,
//                Created = x.Created.DateTime,
//                CreatedBy = x.CreatedBy,
//                LastModified = x.LastModified.DateTime,
//                LastModifiedBy = x.LastModifiedBy
//            })
//            .ToListAsync(cancellationToken);

//        return new PaginatedList<DeviceDto>(devices, devices.Count, request.PageNumber, request.PageSize);
//    }
//}







