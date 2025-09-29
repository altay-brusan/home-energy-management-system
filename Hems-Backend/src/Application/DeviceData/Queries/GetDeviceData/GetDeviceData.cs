using System.Text.Json;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Mappings;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.DeviceData.Queries.GetDeviceData;

public record GetDeviceDataQuery : IRequest<PaginatedList<DeviceDataDto>>
{
    public Guid? DeviceId { get; init; }
    public string? DataType { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? UserId { get; init; } // Authorization için
}

public class GetDeviceDataQueryHandler : IRequestHandler<GetDeviceDataQuery, PaginatedList<DeviceDataDto>>
{
    private readonly IApplicationDbContext _context;

    public GetDeviceDataQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<DeviceDataDto>> Handle(GetDeviceDataQuery request, CancellationToken cancellationToken)
    {
        var query = _context.DeviceData
            .Include(dd => dd.Device)
                .ThenInclude(d => d.SetupDevice!)
                .ThenInclude(sd => sd.Setup!)
                .ThenInclude(s => s.Site!)
            .AsQueryable();

        // Authorization: Sadece kullanıcının kendi device'larının verilerini göster
        if (!string.IsNullOrEmpty(request.UserId))
        {
            query = query.Where(x => x.Device.SetupDevice!.Setup!.Site!.UserId == request.UserId);
        }

        if (request.DeviceId.HasValue)
        {
            query = query.Where(x => x.DeviceId == request.DeviceId.Value);
        }

        if (!string.IsNullOrEmpty(request.DataType))
        {
            query = query.Where(x => x.DataType == request.DataType);
        }

        return await query
            .OrderByDescending(x => x.RequestDate)
            .Select(x => new DeviceDataDto
            {
                Id = x.Id,
                DataType = x.DataType,
                DeviceId = x.DeviceId,
                RequestDate = x.RequestDate,
                Data = x.Data,
                Created = x.Created,
                CreatedBy = x.CreatedBy
            })
            .PaginatedListAsync(request.PageNumber, request.PageSize);
    }
}

public class DeviceDataDto
{
    public int Id { get; set; }
    public string DataType { get; set; } = string.Empty;
    public Guid DeviceId { get; set; }
    public DateTime RequestDate { get; set; }
    public JsonDocument Data { get; set; } = JsonDocument.Parse("{}");
    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }
}
