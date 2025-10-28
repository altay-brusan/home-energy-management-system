using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Devices.Queries.CheckDeviceAccess;
using Hems_Backend.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Hems_Backend.Application.Dashboard.Queries.GetBatteryComponent;

public class GetBatteryComponentQueryHandler : IRequestHandler<GetBatteryComponentQuery, BatteryComponentDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ISender _sender;

    public GetBatteryComponentQueryHandler(IApplicationDbContext context, ISender sender)
    {
        _context = context;
        _sender = sender;
    }

    public async Task<BatteryComponentDto> Handle(GetBatteryComponentQuery request, CancellationToken cancellationToken)
    {
        // Device access kontrolü
        var hasAccess = await _sender.Send(new CheckDeviceAccessQuery { DeviceId = request.DeviceId }, cancellationToken);
        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("Bu cihaza erişim yetkiniz bulunmamaktadır.");
        }

        var today = DateTime.UtcNow.Date;
        var todayUtc = DateTime.SpecifyKind(today, DateTimeKind.Utc);

        // 1. SOC - Son BatteryClusterData'dan
        var soc = await GetSOCFromBatteryClusterDataAsync(request.DeviceId, cancellationToken);

        // 2. Today - Bugünün son DailyCumulativeEnergyData'sından
        var todayCharge = await GetTodayBatteryChargeAsync(request.DeviceId, todayUtc, cancellationToken);

        // 3. Total - Tüm LifetimeEnergyData'ların toplamı
        var totalCharge = await GetTotalBatteryChargeAsync(request.DeviceId, cancellationToken);

        return new BatteryComponentDto
        {
            SOC = Math.Round(soc, 3),
            Today = Math.Round(todayCharge, 3),
            Total = Math.Round(totalCharge, 3)
        };
    }

    private async Task<double> GetSOCFromBatteryClusterDataAsync(Guid deviceId, CancellationToken cancellationToken)
    {
        var batteryData = await _context.DeviceData
            .Where(x => x.DeviceId == deviceId && 
                       x.DataType == "BatteryClusterData")
            .OrderByDescending(x => x.RequestDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (batteryData == null)
            return 0;

        try
        {
            var jsonData = batteryData.Data.RootElement;
            if (jsonData.TryGetProperty("SOC", out var socElement) && 
                socElement.ValueKind == JsonValueKind.Number)
            {
                return socElement.GetDouble();
            }
        }
        catch (Exception)
        {
            // JSON parse hatası durumunda 0 döndür
        }

        return 0;
    }

    private async Task<double> GetTodayBatteryChargeAsync(Guid deviceId, DateTime todayUtc, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var startOfDay = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, DateTimeKind.Utc);
        var endOfDay = startOfDay.AddDays(1);
        
        var todayData = await _context.DeviceEnergyData
            .Where(x => x.DeviceId == deviceId && 
                       x.DataDateTime >= startOfDay &&
                       x.DataDateTime < endOfDay &&
                       x.DataType == "DailyCumulativeEnergyData")
            .OrderByDescending(x => x.DataDateTime)
            .FirstOrDefaultAsync(cancellationToken);

        return todayData?.BatteryChargeEnergyKWh ?? 0;
    }

    private async Task<double> GetTotalBatteryChargeAsync(Guid deviceId, CancellationToken cancellationToken)
    {
        var totalData = await _context.DeviceEnergyData
            .Where(x => x.DeviceId == deviceId && 
                       x.DataType == "LifetimeEnergyData")
            .SumAsync(x => x.BatteryChargeEnergyKWh ?? 0, cancellationToken);

        return totalData;
    }
}
