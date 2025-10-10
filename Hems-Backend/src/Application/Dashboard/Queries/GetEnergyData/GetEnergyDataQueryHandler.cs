using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Devices.Queries.CheckDeviceAccess;
using Hems_Backend.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Dashboard.Queries.GetEnergyData;

public class GetEnergyDataQueryHandler : IRequestHandler<GetEnergyDataQuery, EnergyDataDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ISender _sender;

    public GetEnergyDataQueryHandler(IApplicationDbContext context, ISender sender)
    {
        _context = context;
        _sender = sender;
    }

    public async Task<EnergyDataDto> Handle(GetEnergyDataQuery request, CancellationToken cancellationToken)
    {
        // Kullanıcının device'a erişim yetkisi var mı kontrol et
        var hasAccess = await _sender.Send(new CheckDeviceAccessQuery { DeviceId = request.DeviceId }, cancellationToken);
        
        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("Bu cihaza erişim yetkiniz bulunmamaktadır.");
        }

        var today = DateTime.UtcNow.Date;
        var isEndDateToday = request.EndDate.Date == today;
        
        // DateTime'ları UTC'ye çevir
        var startDate = DateTime.SpecifyKind(request.StartDate, DateTimeKind.Utc);
        var endDate = DateTime.SpecifyKind(request.EndDate, DateTimeKind.Utc);

        var energyData = await GetEnergyDataAsync(request.DeviceId, startDate, endDate, isEndDateToday, cancellationToken);
        
        return CalculateEnergyDataDto(energyData, request.EndDate);
    }

    private async Task<List<DeviceEnergyData>> GetEnergyDataAsync(
        Guid deviceId, 
        DateTime startDate, 
        DateTime endDate, 
        bool isEndDateToday, 
        CancellationToken cancellationToken)
    {
        var query = _context.DeviceEnergyData
            .Where(x => x.DeviceId == deviceId);

        if (isEndDateToday)
        {
            // Son gün bugün ise: önceki günler Lifetime, bugün Daily
            var previousDays = await query
                .Where(x => x.DataDateTime.Date >= startDate.Date && 
                           x.DataDateTime.Date < endDate.Date &&
                           x.DataType == "LifetimeEnergyData")
                .ToListAsync(cancellationToken);

            var todayData = await query
                .Where(x => x.DataDateTime.Date == endDate.Date && 
                           x.DataType == "DailyCumulativeEnergyData")
                .OrderByDescending(x => x.DataDateTime)
                .FirstOrDefaultAsync(cancellationToken);

            var result = previousDays.ToList();
            if (todayData != null)
                result.Add(todayData);

            return result;
        }
        else
        {
            // Son gün bugün değilse: tüm günler Lifetime
            return await query
                .Where(x => x.DataDateTime.Date >= startDate.Date && 
                           x.DataDateTime.Date <= endDate.Date &&
                           x.DataType == "LifetimeEnergyData")
                .ToListAsync(cancellationToken);
        }
    }

    private static EnergyDataDto CalculateEnergyDataDto(List<DeviceEnergyData> energyData, DateTime endDate)
    {
        if (!energyData.Any())
            return CreateEmptyDto(endDate);

        var total = energyData.Aggregate(new
        {
            PvEnergyKWh = 0.0,
            GridImportEnergyKWh = 0.0,
            GridExportEnergyKWh = 0.0,
            GeneratorEnergyKWh = 0.0,
            BatteryDischargeEnergyKWh = 0.0,
            BatteryChargeEnergyKWh = 0.0,
            LoadEnergyKWh = 0.0
        }, (acc, data) => new
        {
            PvEnergyKWh = acc.PvEnergyKWh + (data.PvEnergyKWh ?? 0),
            GridImportEnergyKWh = acc.GridImportEnergyKWh + (data.GridImportEnergyKWh ?? 0),
            GridExportEnergyKWh = acc.GridExportEnergyKWh + (data.GridExportEnergyKWh ?? 0),
            GeneratorEnergyKWh = acc.GeneratorEnergyKWh + (data.GeneratorEnergyKWh ?? 0),
            BatteryDischargeEnergyKWh = acc.BatteryDischargeEnergyKWh + (data.BatteryDischargeEnergyKWh ?? 0),
            BatteryChargeEnergyKWh = acc.BatteryChargeEnergyKWh + (data.BatteryChargeEnergyKWh ?? 0),
            LoadEnergyKWh = acc.LoadEnergyKWh + (data.LoadEnergyKWh ?? 0)
        });

        var totalEnergyInput = total.PvEnergyKWh + total.GridImportEnergyKWh + 
                              total.GeneratorEnergyKWh + total.BatteryChargeEnergyKWh;
        
        var totalEnergyOutput = total.GridExportEnergyKWh + total.BatteryDischargeEnergyKWh + 
                               total.LoadEnergyKWh;

        return new EnergyDataDto
        {
            Date = endDate,
            PvEnergyKWh = total.PvEnergyKWh,
            GridImportEnergyKWh = total.GridImportEnergyKWh,
            GridExportEnergyKWh = total.GridExportEnergyKWh,
            GeneratorEnergyKWh = total.GeneratorEnergyKWh,
            BatteryDischargeEnergyKWh = total.BatteryDischargeEnergyKWh,
            BatteryChargeEnergyKWh = total.BatteryChargeEnergyKWh,
            LoadEnergyKWh = total.LoadEnergyKWh,
            TotalEnergyInput = totalEnergyInput,
            TotalEnergyOutput = totalEnergyOutput,
            PvEnergyPercentage = CalculatePercentage(total.PvEnergyKWh, totalEnergyInput),
            GridImportEnergyPercentage = CalculatePercentage(total.GridImportEnergyKWh, totalEnergyInput),
            GeneratorEnergyPercentage = CalculatePercentage(total.GeneratorEnergyKWh, totalEnergyInput),
            BatteryChargeEnergyPercentage = CalculatePercentage(total.BatteryChargeEnergyKWh, totalEnergyInput),
            GridExportEnergyPercentage = CalculatePercentage(total.GridExportEnergyKWh, totalEnergyOutput),
            BatteryDischargeEnergyPercentage = CalculatePercentage(total.BatteryDischargeEnergyKWh, totalEnergyOutput),
            LoadEnergyPercentage = CalculatePercentage(total.LoadEnergyKWh, totalEnergyOutput)
        };
    }

    private static double CalculatePercentage(double value, double total) =>
        total > 0 ? Math.Round((value / total) * 100, 2) : 0;

    private static EnergyDataDto CreateEmptyDto(DateTime date) => new()
    {
        Date = date,
        PvEnergyKWh = 0,
        GridImportEnergyKWh = 0,
        GridExportEnergyKWh = 0,
        GeneratorEnergyKWh = 0,
        BatteryDischargeEnergyKWh = 0,
        BatteryChargeEnergyKWh = 0,
        LoadEnergyKWh = 0,
        TotalEnergyInput = 0,
        TotalEnergyOutput = 0,
        PvEnergyPercentage = 0,
        GridImportEnergyPercentage = 0,
        GeneratorEnergyPercentage = 0,
        BatteryChargeEnergyPercentage = 0,
        GridExportEnergyPercentage = 0,
        BatteryDischargeEnergyPercentage = 0,
        LoadEnergyPercentage = 0
    };
}
