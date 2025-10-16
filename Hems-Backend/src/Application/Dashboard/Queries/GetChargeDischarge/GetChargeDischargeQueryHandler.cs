using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.Devices.Queries.CheckDeviceAccess;
using Hems_Backend.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Dashboard.Queries.GetChargeDischarge;

public class GetChargeDischargeQueryHandler : IRequestHandler<GetChargeDischargeQuery, ChargeDischargeResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ISender _sender;

    public GetChargeDischargeQueryHandler(IApplicationDbContext context, ISender sender)
    {
        _context = context;
        _sender = sender;
    }

    public async Task<ChargeDischargeResponseDto> Handle(GetChargeDischargeQuery request, CancellationToken cancellationToken)
    {
        // Device access kontrolü
        var hasAccess = await _sender.Send(new CheckDeviceAccessQuery { DeviceId = request.DeviceId }, cancellationToken);
        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("Bu cihaza erişim yetkiniz bulunmamaktadır.");
        }

        var today = DateTime.UtcNow.Date;
        var todayUtc = DateTime.SpecifyKind(today, DateTimeKind.Utc);

        // Bugünün DailyCumulativeEnergyData verilerini çek
        var energyData = await _context.DeviceEnergyData
            .Where(x => x.DeviceId == request.DeviceId && 
                       x.DataDateTime.Date == todayUtc.Date &&
                       x.DataType == "DailyCumulativeEnergyData")
            .OrderBy(x => x.DataDateTime)
            .ToListAsync(cancellationToken);

        if (!energyData.Any())
        {
            return new ChargeDischargeResponseDto
            {
                Range = "Day",
                Data = new List<Hems_Backend.Application.Common.Models.ChargeDischargeDataPoint>(),
                Statistics = new ChargeDischargeStatistics
                {
                    AverageCharge = 0,
                    PeakCharge = 0,
                    CurrentCharge = 0,
                    AverageDischarge = 0,
                    PeakDischarge = 0,
                    CurrentDischarge = 0,
                    Unit = "kW"
                }
            };
        }

        // Saatlik gruplama ve power hesaplama (son değerleri al)
        var dataPoints = energyData
            .GroupBy(x => x.DataDateTime.Hour)
            .Select(g => new Hems_Backend.Application.Dashboard.Queries.GetChargeDischarge.ChargeDischargeDataPoint
            {
                DateTime = g.First().DataDateTime,
                ChargePowerKw = Math.Round(g.OrderByDescending(x => x.DataDateTime).First().BatteryChargeEnergyKWh ?? 0, 3),
                DischargePowerKw = Math.Round(g.OrderByDescending(x => x.DataDateTime).First().BatteryDischargeEnergyKWh ?? 0, 3),
                Label = $"Hour-{g.Key:00}:00"
            })
            .OrderBy(x => x.DateTime)
            .ToList();

        // İstatistikleri hesapla
        var chargeValues = dataPoints.Select(x => x.ChargePowerKw).ToList();
        var dischargeValues = dataPoints.Select(x => x.DischargePowerKw).ToList();

        var averageChargePowerKw = chargeValues.Any() ? Math.Round(chargeValues.Average(), 3) : 0;
        var peakChargePowerKw = chargeValues.Any() ? Math.Round(chargeValues.Max(), 3) : 0;
        var currentChargePowerKw = chargeValues.Any() ? Math.Round(chargeValues.Last(), 3) : 0;

        var averageDischargePowerKw = dischargeValues.Any() ? Math.Round(dischargeValues.Average(), 3) : 0;
        var peakDischargePowerKw = dischargeValues.Any() ? Math.Round(dischargeValues.Max(), 3) : 0;
        var currentDischargePowerKw = dischargeValues.Any() ? Math.Round(dischargeValues.Last(), 3) : 0;

        // Yeni DTO formatına çevir
        var newDataPoints = dataPoints.Select(x => new Hems_Backend.Application.Common.Models.ChargeDischargeDataPoint
        {
            Timestamp = x.Label,
            ChargeValue = x.ChargePowerKw,
            DischargeValue = x.DischargePowerKw,
            Unit = "kW"
        }).ToList();

        return new ChargeDischargeResponseDto
        {
            Range = "Day",
            Data = newDataPoints,
            Statistics = new ChargeDischargeStatistics
            {
                AverageCharge = averageChargePowerKw,
                PeakCharge = peakChargePowerKw,
                CurrentCharge = currentChargePowerKw,
                AverageDischarge = averageDischargePowerKw,
                PeakDischarge = peakDischargePowerKw,
                CurrentDischarge = currentDischargePowerKw,
                Unit = "kW"
            }
        };
    }
}
