using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.DeviceData.Commands.CreateDeviceEnergyData;

public record CreateDeviceEnergyDataCommand : IRequest<int>
{
    public string DataType { get; init; } = string.Empty;
    public Guid DeviceId { get; init; }
    public DateTime DataDateTime { get; init; }
    
    // Cihazdan gelen veriler (Today/Total formatında)
    public double? PvEnergyKWhToday { get; init; }
    public double? GridImportEnergyKWhToday { get; init; }
    public double? GridExportEnergyKWhToday { get; init; }
    public double? GeneratorEnergyKWhToday { get; init; }
    public double? BatteryDischargeEnergyKWhToday { get; init; }
    public double? BatteryChargeEnergyKWhToday { get; init; }
    public double? LoadEnergyKWhToday { get; init; }
    
    public double? PvEnergyKWhTotal { get; init; }
    public double? GridImportEnergyKWhTotal { get; init; }
    public double? GridExportEnergyKWhTotal { get; init; }
    public double? GeneratorEnergyKWhTotal { get; init; }
    public double? BatteryDischargeEnergyKWhTotal { get; init; }
    public double? BatteryChargeEnergyKWhTotal { get; init; }
    public double? LoadEnergyKWhTotal { get; init; }
}

public class CreateDeviceEnergyDataCommandHandler : IRequestHandler<CreateDeviceEnergyDataCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateDeviceEnergyDataCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreateDeviceEnergyDataCommand request, CancellationToken cancellationToken)
    {
        // Device'ın var olup olmadığını kontrol et
        var deviceExists = await _context.Devices
            .AnyAsync(d => d.Id == request.DeviceId && !d.IsDeleted, cancellationToken);

        if (!deviceExists)
        {
            throw new ArgumentException($"Device with ID {request.DeviceId} not found.");
        }

        // Aynı device, datetime ve data type için kayıt var mı kontrol et
        var existingRecord = await _context.DeviceEnergyData
            .FirstOrDefaultAsync(x => 
                x.DeviceId == request.DeviceId && 
                x.DataDateTime == request.DataDateTime && 
                x.DataType == request.DataType, 
                cancellationToken);

        if (existingRecord != null)
        {
            // Mevcut kaydı güncelle - DataType'a göre Today veya Total değerini kullan
            existingRecord.PvEnergyKWh = request.DataType == "DailyCumulativeEnergyData" ? request.PvEnergyKWhToday : request.PvEnergyKWhTotal;
            existingRecord.GridImportEnergyKWh = request.DataType == "DailyCumulativeEnergyData" ? request.GridImportEnergyKWhToday : request.GridImportEnergyKWhTotal;
            existingRecord.GridExportEnergyKWh = request.DataType == "DailyCumulativeEnergyData" ? request.GridExportEnergyKWhToday : request.GridExportEnergyKWhTotal;
            existingRecord.GeneratorEnergyKWh = request.DataType == "DailyCumulativeEnergyData" ? request.GeneratorEnergyKWhToday : request.GeneratorEnergyKWhTotal;
            existingRecord.BatteryDischargeEnergyKWh = request.DataType == "DailyCumulativeEnergyData" ? request.BatteryDischargeEnergyKWhToday : request.BatteryDischargeEnergyKWhTotal;
            existingRecord.BatteryChargeEnergyKWh = request.DataType == "DailyCumulativeEnergyData" ? request.BatteryChargeEnergyKWhToday : request.BatteryChargeEnergyKWhTotal;
            existingRecord.LoadEnergyKWh = request.DataType == "DailyCumulativeEnergyData" ? request.LoadEnergyKWhToday : request.LoadEnergyKWhTotal;
            
            existingRecord.LastModified = DateTime.UtcNow;
            existingRecord.LastModifiedBy = "DeviceSimulator";
        }
        else
        {
            // Yeni kayıt oluştur - DataType'a göre Today veya Total değerini kullan
            var energyData = new DeviceEnergyData
            {
                DeviceId = request.DeviceId,
                DataDateTime = request.DataDateTime,
                DataType = request.DataType,
                
                PvEnergyKWh = request.DataType == "DailyCumulativeEnergyData" ? request.PvEnergyKWhToday : request.PvEnergyKWhTotal,
                GridImportEnergyKWh = request.DataType == "DailyCumulativeEnergyData" ? request.GridImportEnergyKWhToday : request.GridImportEnergyKWhTotal,
                GridExportEnergyKWh = request.DataType == "DailyCumulativeEnergyData" ? request.GridExportEnergyKWhToday : request.GridExportEnergyKWhTotal,
                GeneratorEnergyKWh = request.DataType == "DailyCumulativeEnergyData" ? request.GeneratorEnergyKWhToday : request.GeneratorEnergyKWhTotal,
                BatteryDischargeEnergyKWh = request.DataType == "DailyCumulativeEnergyData" ? request.BatteryDischargeEnergyKWhToday : request.BatteryDischargeEnergyKWhTotal,
                BatteryChargeEnergyKWh = request.DataType == "DailyCumulativeEnergyData" ? request.BatteryChargeEnergyKWhToday : request.BatteryChargeEnergyKWhTotal,
                LoadEnergyKWh = request.DataType == "DailyCumulativeEnergyData" ? request.LoadEnergyKWhToday : request.LoadEnergyKWhTotal,
                
                Created = DateTime.UtcNow,
                CreatedBy = "DeviceSimulator"
            };

            _context.DeviceEnergyData.Add(energyData);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return existingRecord?.Id ?? 0;
    }
}
