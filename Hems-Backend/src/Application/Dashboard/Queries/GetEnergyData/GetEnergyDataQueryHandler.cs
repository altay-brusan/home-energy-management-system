//using Hems_Backend.Application.Common.Interfaces;
//using Hems_Backend.Application.Common.Models;
//using Hems_Backend.Application.Devices.Queries.CheckDeviceAccess;
//using Hems_Backend.Domain.Entities;
//using MediatR;
//using Microsoft.EntityFrameworkCore;

//namespace Hems_Backend.Application.Dashboard.Queries.GetEnergyData;

//public class GetEnergyDataQueryHandler : IRequestHandler<GetEnergyDataQuery, EnergyFlowResponseDto>
//{
//    private readonly IApplicationDbContext _context;
//    private readonly ISender _sender;
//    private readonly IDeviceMappingService _deviceMapping;

//    public GetEnergyDataQueryHandler(IApplicationDbContext context, ISender sender, IDeviceMappingService deviceMapping)
//    {
//        _context = context;
//        _sender = sender;
//        _deviceMapping = deviceMapping;
//    }

//    public async Task<EnergyFlowResponseDto> Handle(GetEnergyDataQuery request, CancellationToken cancellationToken)
//    {
//        // Kullanıcının device'a erişim yetkisi var mı kontrol et
//        var hasAccess = await _sender.Send(new CheckDeviceAccessQuery { DeviceId = request.DeviceId }, cancellationToken);
        
//        if (!hasAccess)
//        {
//            throw new UnauthorizedAccessException("Bu cihaza erişim yetkiniz bulunmamaktadır.");
//        }

//        // Map local DeviceId to ThingsBoardDeviceId
//        var thingsBoardDeviceId = await _deviceMapping.GetThingsBoardDeviceIdAsync(request.DeviceId, cancellationToken);
//        if (thingsBoardDeviceId == null)
//        {
//            throw new ArgumentException($"Device with ID {request.DeviceId} not found or has no ThingsBoard mapping.");
//        }

//        var today = DateTime.UtcNow.Date;
//        var isEndDateToday = request.EndDate.Date == today;
        
//        // DateTime'ı UTC'ye çevir
//        var startDate = DateTime.SpecifyKind(request.StartDate, DateTimeKind.Utc);
//        var endDate = DateTime.SpecifyKind(request.EndDate, DateTimeKind.Utc);

//        var energyData = await GetEnergyDataAsync(thingsBoardDeviceId.Value, startDate, endDate, isEndDateToday, cancellationToken);
        
//        return CalculateEnergyFlowResponseDto(energyData, request.EndDate);
//    }

//    private async Task<List<DeviceEnergyData>> GetEnergyDataAsync(
//        Guid thingsBoardDeviceId, 
//        DateTime startDate, 
//        DateTime endDate, 
//        bool isEndDateToday, 
//        CancellationToken cancellationToken)
//    {
//        var query = _context.DeviceEnergyData
//            .Where(x => x.ThingsBoardDeviceId == thingsBoardDeviceId);

//        if (isEndDateToday)
//        {
//            // Son gün bugün ise: önceki günler Lifetime, bugün Daily
//            var previousDays = await query
//                .Where(x => x.DeviceLocalTime.Date >= startDate.Date && 
//                           x.DeviceLocalTime.Date < endDate.Date &&
//                           x.DataType == "LifetimeEnergyData")
//                .ToListAsync(cancellationToken);

//            var todayData = await query
//                .Where(x => x.DeviceLocalTime.Date == endDate.Date && 
//                           x.DataType == "DailyCumulativeEnergyData")
//                .OrderByDescending(x => x.DeviceLocalTime)
//                .FirstOrDefaultAsync(cancellationToken);

//            var result = previousDays.ToList();
//            if (todayData != null)
//                result.Add(todayData);

//            return result;
//        }
//        else
//        {
//            // Son gün bugün değilse: tüm günler Lifetime
//            return await query
//                .Where(x => x.DeviceLocalTime.Date >= startDate.Date && 
//                           x.DeviceLocalTime.Date <= endDate.Date &&
//                           x.DataType == "LifetimeEnergyData")
//                .ToListAsync(cancellationToken);
//        }
//    }

//    private static EnergyFlowResponseDto CalculateEnergyFlowResponseDto(List<DeviceEnergyData> energyData, DateTime endDate)
//    {
//        if (!energyData.Any())
//            return CreateEmptyEnergyFlowDto(endDate);

//        var total = energyData.Aggregate(new
//        {
//            PvEnergyKWh = 0.0,
//            GridImportEnergyKWh = 0.0,
//            GridExportEnergyKWh = 0.0,
//            GeneratorEnergyKWh = 0.0,
//            BatteryDischargeEnergyKWh = 0.0,
//            BatteryChargeEnergyKWh = 0.0,
//            LoadEnergyKWh = 0.0
//        }, (acc, data) => new
//        {
//            PvEnergyKWh = acc.PvEnergyKWh + (data.PvTotalEnergyKWh ?? 0),
//            GridImportEnergyKWh = acc.GridImportEnergyKWh + (data.GridImportEnergyKWh ?? 0),
//            GridExportEnergyKWh = acc.GridExportEnergyKWh + (data.GridExportEnergyKWh ?? 0),
//            GeneratorEnergyKWh = acc.GeneratorEnergyKWh + (data.GeneratorEnergyKWh ?? 0),
//            BatteryDischargeEnergyKWh = acc.BatteryDischargeEnergyKWh + (data.BatteryDischargeEnergyKWh ?? 0),
//            BatteryChargeEnergyKWh = acc.BatteryChargeEnergyKWh + (data.BatteryChargeEnergyKWh ?? 0),
//            LoadEnergyKWh = acc.LoadEnergyKWh + (data.LoadEnergyKWh ?? 0)
//        });

//        var totalEnergyInput = total.PvEnergyKWh + total.GridImportEnergyKWh + 
//                              total.GeneratorEnergyKWh + total.BatteryChargeEnergyKWh;
        
//        var totalEnergyOutput = total.GridExportEnergyKWh + total.BatteryDischargeEnergyKWh + 
//                               total.LoadEnergyKWh;

//        // Sankey diagram formatına çevir
//        var sources = new List<EnergySource>();
//        var destinations = new List<EnergyDestination>();
//        var flows = new List<EnergyFlow>();

//        // Sources (Enerji kaynakları)
//        if (total.PvEnergyKWh > 0)
//        {
//            sources.Add(new EnergySource
//            {
//                Id = "PV",
//                Label = "PV",
//                Value = Math.Round(total.PvEnergyKWh, 2),
//                Percentage = CalculatePercentage(total.PvEnergyKWh, totalEnergyInput),
//                Color = "#FDB52E"
//            });
//        }

//        if (total.GridImportEnergyKWh > 0)
//        {
//            sources.Add(new EnergySource
//            {
//                Id = "GRID",
//                Label = "GRID",
//                Value = Math.Round(total.GridImportEnergyKWh, 2),
//                Percentage = CalculatePercentage(total.GridImportEnergyKWh, totalEnergyInput),
//                Color = "#4ECDC4"
//            });
//        }

//        if (total.GeneratorEnergyKWh > 0)
//        {
//            sources.Add(new EnergySource
//            {
//                Id = "GENERATOR",
//                Label = "GENERATOR",
//                Value = Math.Round(total.GeneratorEnergyKWh, 2),
//                Percentage = CalculatePercentage(total.GeneratorEnergyKWh, totalEnergyInput),
//                Color = "#45B7D1"
//            });
//        }

//        if (total.BatteryChargeEnergyKWh > 0)
//        {
//            sources.Add(new EnergySource
//            {
//                Id = "BATTERY_CHARGE",
//                Label = "BATTERY CHARGE",
//                Value = Math.Round(total.BatteryChargeEnergyKWh, 2),
//                Percentage = CalculatePercentage(total.BatteryChargeEnergyKWh, totalEnergyInput),
//                Color = "#96CEB4"
//            });
//        }

//        // Destinations (Enerji çıkışları)
//        if (total.LoadEnergyKWh > 0)
//        {
//            destinations.Add(new EnergyDestination
//            {
//                Id = "LOAD",
//                Label = "LOAD",
//                Value = Math.Round(total.LoadEnergyKWh, 2),
//                Percentage = CalculatePercentage(total.LoadEnergyKWh, totalEnergyOutput),
//                Color = "#FF6B6B"
//            });
//        }

//        if (total.GridExportEnergyKWh > 0)
//        {
//            destinations.Add(new EnergyDestination
//            {
//                Id = "GRID_EXPORT",
//                Label = "GRID EXPORT",
//                Value = Math.Round(total.GridExportEnergyKWh, 2),
//                Percentage = CalculatePercentage(total.GridExportEnergyKWh, totalEnergyOutput),
//                Color = "#4ECDC4"
//            });
//        }

//        if (total.BatteryDischargeEnergyKWh > 0)
//        {
//            destinations.Add(new EnergyDestination
//            {
//                Id = "BATTERY_DISCHARGE",
//                Label = "BATTERY DISCHARGE",
//                Value = Math.Round(total.BatteryDischargeEnergyKWh, 2),
//                Percentage = CalculatePercentage(total.BatteryDischargeEnergyKWh, totalEnergyOutput),
//                Color = "#96CEB4"
//            });
//        }

//        // Flows (Enerji akışları)
//        foreach (var source in sources)
//        {
//            flows.Add(new EnergyFlow
//            {
//                Source = source.Id,
//                Target = "CENTER",
//                Value = source.Value
//            });
//        }

//        foreach (var destination in destinations)
//        {
//            flows.Add(new EnergyFlow
//            {
//                Source = "CENTER",
//                Target = destination.Id,
//                Value = destination.Value
//            });
//        }

//        return new EnergyFlowResponseDto
//        {
//            Date = endDate.ToString("yyyy-MM-dd"),
//            Range = "Day",
//            Sources = sources,
//            Destinations = destinations,
//            Flows = flows
//        };
//    }

//    private static double CalculatePercentage(double value, double total) =>
//        total > 0 ? Math.Round((value / total) * 100, 2) : 0;

//    private static EnergyFlowResponseDto CreateEmptyEnergyFlowDto(DateTime date) => new()
//    {
//        Date = date.ToString("yyyy-MM-dd"),
//        Range = "Day",
//        Sources = new List<EnergySource>(),
//        Destinations = new List<EnergyDestination>(),
//        Flows = new List<EnergyFlow>()
//    };
//}
