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
    private readonly IWeatherService _weatherService;

    public CreateDeviceEnergyDataCommandHandler(IApplicationDbContext context, IWeatherService weatherService)
    {
        _context = context;
        _weatherService = weatherService;
    }

    public async Task<int> Handle(CreateDeviceEnergyDataCommand request, CancellationToken cancellationToken)
    {
        // Device'ı ve Site bilgilerini al
        var device = await _context.Devices
            .Include(d => d.SetupDevice!)
            .ThenInclude(sd => sd.Setup!)
            .ThenInclude(s => s.Site!)
            .FirstOrDefaultAsync(d => d.Id == request.DeviceId && !d.IsDeleted, cancellationToken);

        if (device == null)
        {
            throw new ArgumentException($"Device with ID {request.DeviceId} not found.");
        }

        var site = device.SetupDevice?.Setup?.Site;
        
        // Hava durumu bilgilerini al
        WeatherData? currentWeather = null;
        if (site?.Latitude.HasValue == true && site.Longitude.HasValue == true)
        {
            currentWeather = await _weatherService.GetCurrentWeatherAsync(
                site.Latitude.Value, 
                site.Longitude.Value, 
                cancellationToken);
        }

        // Aynı device, datetime ve data type için kayıt var mı kontrol et
        var existingRecord = await _context.DeviceEnergyData
            .FirstOrDefaultAsync(x => 
                x.ThingsBoardDeviceId == request.DeviceId && 
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
            
            // Hava durumu bilgilerini güncelle
            if (currentWeather != null)
            {
                existingRecord.WeatherTemperature = currentWeather.Temperature;
                existingRecord.WeatherTemperatureCondition = currentWeather.Description;
            }
            
            existingRecord.LastModified = DateTime.UtcNow;
            existingRecord.LastModifiedBy = "DeviceSimulator";
        }
        else
        {
            // Yeni kayıt oluştur - DataType'a göre Today veya Total değerini kullan
            var energyData = new DeviceEnergyData
            {
                ThingsBoardDeviceId = request.DeviceId,
                DataDateTime = request.DataDateTime,
                DataType = request.DataType,
                
                PvEnergyKWh = request.DataType == "DailyCumulativeEnergyData" ? request.PvEnergyKWhToday : request.PvEnergyKWhTotal,
                GridImportEnergyKWh = request.DataType == "DailyCumulativeEnergyData" ? request.GridImportEnergyKWhToday : request.GridImportEnergyKWhTotal,
                GridExportEnergyKWh = request.DataType == "DailyCumulativeEnergyData" ? request.GridExportEnergyKWhToday : request.GridExportEnergyKWhTotal,
                GeneratorEnergyKWh = request.DataType == "DailyCumulativeEnergyData" ? request.GeneratorEnergyKWhToday : request.GeneratorEnergyKWhTotal,
                BatteryDischargeEnergyKWh = request.DataType == "DailyCumulativeEnergyData" ? request.BatteryDischargeEnergyKWhToday : request.BatteryDischargeEnergyKWhTotal,
                BatteryChargeEnergyKWh = request.DataType == "DailyCumulativeEnergyData" ? request.BatteryChargeEnergyKWhToday : request.BatteryChargeEnergyKWhTotal,
                LoadEnergyKWh = request.DataType == "DailyCumulativeEnergyData" ? request.LoadEnergyKWhToday : request.LoadEnergyKWhTotal,
                
                // Hava durumu bilgilerini ekle
                WeatherTemperature = currentWeather?.Temperature,
                WeatherTemperatureCondition = currentWeather?.Description,
                
                Created = DateTime.UtcNow,
                CreatedBy = "DeviceSimulator"
            };

            _context.DeviceEnergyData.Add(energyData);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return existingRecord?.Id ?? 0;
    }
}
