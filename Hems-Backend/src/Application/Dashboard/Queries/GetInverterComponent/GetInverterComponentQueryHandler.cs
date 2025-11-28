using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Devices.Queries.CheckDeviceAccess;
using Hems_Backend.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Dashboard.Queries.GetInverterComponent;

public class GetInverterComponentQueryHandler : IRequestHandler<GetInverterComponentQuery, InverterComponentDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ISender _sender;
    private readonly IDeviceMappingService _deviceMapping;

    public GetInverterComponentQueryHandler(IApplicationDbContext context, ISender sender, IDeviceMappingService deviceMapping)
    {
        _context = context;
        _sender = sender;
        _deviceMapping = deviceMapping;
    }

    public async Task<InverterComponentDto> Handle(GetInverterComponentQuery request, CancellationToken cancellationToken)
    {
        // Device access kontrolü
        var hasAccess = await _sender.Send(new CheckDeviceAccessQuery { DeviceId = request.DeviceId }, cancellationToken);
        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("Bu cihaza erişim yetkiniz bulunmamaktadır.");
        }

        // Map local DeviceId to ThingsBoardDeviceId
        var thingsBoardDeviceId = await _deviceMapping.GetThingsBoardDeviceIdAsync(request.DeviceId, cancellationToken);
        if (thingsBoardDeviceId == null)
        {
            throw new ArgumentException($"Device with ID {request.DeviceId} not found or has no ThingsBoard mapping.");
        }

        var today = DateTime.UtcNow.Date;
        var todayUtc = DateTime.SpecifyKind(today, DateTimeKind.Utc);

        // 1. Power - Son hourly veriden power hesapla
        var (power, powerUnit) = await CalculatePowerFromHourlyDataAsync(thingsBoardDeviceId.Value, cancellationToken);

        // 2. Today - Bugünün son DailyCumulativeEnergyData'sından
        var todayEnergy = await GetTodayInverterEnergyAsync(thingsBoardDeviceId.Value, todayUtc, cancellationToken);

        // 3. Total - Son LifetimeEnergyData'dan
        var totalEnergy = await GetTotalInverterEnergyAsync(thingsBoardDeviceId.Value, cancellationToken);

        return new InverterComponentDto
        {
            Power = Math.Round(power, 3),
            PowerUnit = powerUnit,
            Today = Math.Round(todayEnergy, 3),
            Total = Math.Round(totalEnergy, 3)
        };
    }

    private async Task<(double power, string unit)> CalculatePowerFromHourlyDataAsync(Guid thingsBoardDeviceId, CancellationToken cancellationToken)
    {
        // Son 2 hourly veriyi al (power hesaplamak için)
        var hourlyData = await _context.DeviceEnergyData
            .Where(x => x.ThingsBoardDeviceId == thingsBoardDeviceId && 
                       x.DataType == "DailyCumulativeEnergyData")
            .OrderByDescending(x => x.DataDateTime)
            .Take(2)
            .ToListAsync(cancellationToken);

        if (hourlyData.Count < 2)
            return (0, "kW");

        var latestData = hourlyData[0];
        var previousData = hourlyData[1];

        // Power hesaplama: Solar (PV) + Battery (deşarj) + Grid + Generator - Battery (şarj) - Load - Grid
        var latestPower = CalculateNetPower(latestData);
        var previousPower = CalculateNetPower(previousData);

        // Saatlik farkı hesapla (kWh -> kW dönüşümü için saat farkına böl)
        var timeDiffHours = (latestData.DataDateTime - previousData.DataDateTime).TotalHours;
        
        if (timeDiffHours <= 0)
            return (0, "kW");

        var powerDiff = latestPower - previousPower;
        var powerInKw = powerDiff / timeDiffHours; // kW cinsinden power

        // Eğer power 1 kW'dan küçükse W cinsinden göster
        if (Math.Abs(powerInKw) < 1.0)
        {
            var powerInW = powerInKw * 1000; // kW'yi W'ye çevir
            return (Math.Round(powerInW, 1), "W");
        }
        else
        {
            return (Math.Round(powerInKw, 3), "kW");
        }
    }

    private double CalculateNetPower(DeviceEnergyData data)
    {
        // Solar (PV) + Battery (deşarj) + Grid + Generator - Battery (şarj) - Load - Grid
        var positiveEnergy = (data.PvTotalEnergyKWh ?? 0) + 
                           (data.BatteryDischargeEnergyKWh ?? 0) + 
                           (data.GridImportEnergyKWh ?? 0) + 
                           (data.GeneratorEnergyKWh ?? 0);

        var negativeEnergy = (data.BatteryChargeEnergyKWh ?? 0) + 
                           (data.LoadEnergyKWh ?? 0) + 
                           (data.GridExportEnergyKWh ?? 0);

        return positiveEnergy - negativeEnergy;
    }

    private double CalculatePositiveEnergyOnly(DeviceEnergyData data)
    {
        // Sadece pozitif enerjileri topla: Solar (PV) + Battery (deşarj) + Grid + Generator
        return (data.PvTotalEnergyKWh ?? 0) + 
               (data.BatteryDischargeEnergyKWh ?? 0) + 
               (data.GridImportEnergyKWh ?? 0) + 
               (data.GeneratorEnergyKWh ?? 0);
    }

    private async Task<double> GetTodayInverterEnergyAsync(Guid thingsBoardDeviceId, DateTime todayUtc, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var startOfDay = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, DateTimeKind.Utc);
        var endOfDay = startOfDay.AddDays(1);
        
        var todayData = await _context.DeviceEnergyData
            .Where(x => x.ThingsBoardDeviceId == thingsBoardDeviceId && 
                       x.DataDateTime >= startOfDay &&
                       x.DataDateTime < endOfDay &&
                       x.DataType == "DailyCumulativeEnergyData")
            .OrderByDescending(x => x.DataDateTime)
            .FirstOrDefaultAsync(cancellationToken);

        if (todayData == null)
            return 0;

        return CalculatePositiveEnergyOnly(todayData);
    }

    private async Task<double> GetTotalInverterEnergyAsync(Guid thingsBoardDeviceId, CancellationToken cancellationToken)
    {
        var totalData = await _context.DeviceEnergyData
            .Where(x => x.ThingsBoardDeviceId == thingsBoardDeviceId && 
                       x.DataType == "LifetimeEnergyData")
            .OrderByDescending(x => x.DataDateTime)
            .FirstOrDefaultAsync(cancellationToken);

        if (totalData == null)
            return 0;

        return CalculatePositiveEnergyOnly(totalData);
    }
}
