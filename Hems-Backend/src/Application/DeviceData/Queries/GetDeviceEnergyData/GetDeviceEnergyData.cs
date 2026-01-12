using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Mappings;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.DeviceData.Queries.GetDeviceEnergyData;

public record GetDeviceEnergyDataQuery : IRequest<PaginatedList<DeviceEnergyDataDto>>
{
    /// <summary>
    /// ThingsBoard Device ID
    /// </summary>
    public Guid DeviceId { get; init; }

    /// <summary>
    /// Data type: "DailyCumulativeEnergyData" or "LifetimeEnergyData"
    /// </summary>
    public string DataType { get; init; } = string.Empty;

    /// <summary>
    /// Optional: Filter by specific date (device local time)
    /// </summary>
    public DateTime? Date { get; init; }

    /// <summary>
    /// Optional: Start date for date range filter
    /// </summary>
    public DateTime? StartDate { get; init; }

    /// <summary>
    /// Optional: End date for date range filter
    /// </summary>
    public DateTime? EndDate { get; init; }

    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public class GetDeviceEnergyDataQueryHandler : IRequestHandler<GetDeviceEnergyDataQuery, PaginatedList<DeviceEnergyDataDto>>
{
    private readonly IApplicationDbContext _context;

    public GetDeviceEnergyDataQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<DeviceEnergyDataDto>> Handle(GetDeviceEnergyDataQuery request, CancellationToken cancellationToken)
    {
        var query = _context.DeviceEnergyData
            .Where(x => x.ThingsBoardDeviceId == request.DeviceId)
            .Where(x => x.DataType == request.DataType)
            .AsQueryable();

        // Filter by specific date
        if (request.Date.HasValue)
        {
            var date = request.Date.Value.Date;
            query = query.Where(x => x.DeviceLocalTime.Date == date);
        }

        // Filter by date range
        if (request.StartDate.HasValue)
        {
            query = query.Where(x => x.DeviceLocalTime >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            query = query.Where(x => x.DeviceLocalTime <= request.EndDate.Value);
        }

        return await query
            .OrderByDescending(x => x.DeviceLocalTime)
            .Select(x => new DeviceEnergyDataDto
            {
                Id = x.Id,
                DeviceId = x.ThingsBoardDeviceId,
                DeviceLocalTime = x.DeviceLocalTime,
                DataType = x.DataType,

                // Load
                LoadEnergyKWh = x.LoadEnergyKWh,

                // Grid
                GridImportEnergyKWh = x.GridImportEnergyKWh,
                GridExportEnergyKWh = x.GridExportEnergyKWh,

                // PV
                PvTotalEnergyKWh = x.PvTotalEnergyKWh,
                PvMppt1EnergyKWh = x.PvMppt1EnergyKWh,
                PvMppt2EnergyKWh = x.PvMppt2EnergyKWh,
                PvMppt3EnergyKWh = x.PvMppt3EnergyKWh,
                PvMppt4EnergyKWh = x.PvMppt4EnergyKWh,

                // Battery
                BatteryChargeEnergyKWh = x.BatteryChargeEnergyKWh,
                BatteryDischargeEnergyKWh = x.BatteryDischargeEnergyKWh,

                // Generator
                GeneratorEnergyKWh = x.GeneratorEnergyKWh,

                // Audit
                Created = x.Created,
                CreatedBy = x.CreatedBy,
                LastModified = x.LastModified,
                LastModifiedBy = x.LastModifiedBy
            })
            .PaginatedListAsync(request.PageNumber, request.PageSize);
    }
}

public class DeviceEnergyDataDto
{
    public int Id { get; set; }
    public Guid DeviceId { get; set; }
    public DateTime DeviceLocalTime { get; set; }
    public string DataType { get; set; } = string.Empty;

    // Load
    public double? LoadEnergyKWh { get; set; }

    // Grid
    public double? GridImportEnergyKWh { get; set; }
    public double? GridExportEnergyKWh { get; set; }

    // PV
    public double? PvTotalEnergyKWh { get; set; }
    public double? PvMppt1EnergyKWh { get; set; }
    public double? PvMppt2EnergyKWh { get; set; }
    public double? PvMppt3EnergyKWh { get; set; }
    public double? PvMppt4EnergyKWh { get; set; }

    // Battery
    public double? BatteryChargeEnergyKWh { get; set; }
    public double? BatteryDischargeEnergyKWh { get; set; }

    // Generator
    public double? GeneratorEnergyKWh { get; set; }

    // Audit
    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
}
