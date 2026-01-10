using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Constants;
using Hems_Backend.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.DeviceData.Commands.CreateDeviceEnergyData;

public record CreateDeviceEnergyDataCommand : IRequest<int>
{
    public string DataType { get; init; } = string.Empty;
    public Guid DeviceId { get; init; }
    public DateTime DeviceLocalTime { get; init; }
    
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
    
    // MPPT individual values
    public double? PvMppt1EnergyKWh { get; init; }
    public double? PvMppt2EnergyKWh { get; init; }
    public double? PvMppt3EnergyKWh { get; init; }
    public double? PvMppt4EnergyKWh { get; init; }

    /// <summary>
    /// Source of the data: "Device" (RPC), "SystemEstimate" (fallback), "Simulator", or "System" (default)
    /// </summary>
    public string? CreatedBy { get; init; }
}

public class CreateDeviceEnergyDataCommandHandler : IRequestHandler<CreateDeviceEnergyDataCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IWeatherService _weatherService;
    private readonly ITimezoneService _timezoneService;
    private readonly IDeviceMappingService _deviceMapping;

    public CreateDeviceEnergyDataCommandHandler(
        IApplicationDbContext context, 
        IWeatherService weatherService,
        ITimezoneService timezoneService,
        IDeviceMappingService deviceMapping)
    {
        _context = context;
        _weatherService = weatherService;
        _timezoneService = timezoneService;
        _deviceMapping = deviceMapping;
    }

    public async Task<int> Handle(CreateDeviceEnergyDataCommand request, CancellationToken cancellationToken)
    {
        // Device'ı ve Site bilgilerini al (DeviceId is actually ThingsBoardDeviceId)
        var device = await _context.Devices
            .Include(d => d.SetupDevice!)
            .ThenInclude(sd => sd.Setup!)
            .ThenInclude(s => s.Site!)
            .FirstOrDefaultAsync(d => d.ThingsBoardDeviceId == request.DeviceId && !d.IsDeleted, cancellationToken);

        if (device == null)
        {
            throw new ArgumentException($"Device with ThingsBoardDeviceId {request.DeviceId} not found.");
        }

        // Convert UTC DeviceLocalTime to device local time using cached timezone service
        var localDeviceId = await _deviceMapping.GetDeviceIdAsync(request.DeviceId, cancellationToken);
        if (localDeviceId == null)
        {
            throw new ArgumentException($"Device mapping not found for ThingsBoardDeviceId {request.DeviceId}");
        }
        
        var localDeviceLocalTime = await _timezoneService.ConvertUtcToDeviceTimeAsync(
            localDeviceId.Value, 
            request.DeviceLocalTime, 
            cancellationToken);

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

        // Aynı device, datetime ve data type için kayıt var mı kontrol et (using local time)
        var existingRecord = await _context.DeviceEnergyData
            .FirstOrDefaultAsync(x => 
                x.ThingsBoardDeviceId == request.DeviceId && 
                x.DeviceLocalTime == localDeviceLocalTime && 
                x.DataType == request.DataType, 
                cancellationToken);

        if (existingRecord != null)
        {
            // Mevcut kaydı güncelle - DataType'a göre Today veya Total değerini kullan
            existingRecord.PvTotalEnergyKWh = request.DataType == DataTypes.DailyCumulativeEnergy ? request.PvEnergyKWhToday : request.PvEnergyKWhTotal;
            existingRecord.GridImportEnergyKWh = request.DataType == DataTypes.DailyCumulativeEnergy ? request.GridImportEnergyKWhToday : request.GridImportEnergyKWhTotal;
            existingRecord.GridExportEnergyKWh = request.DataType == DataTypes.DailyCumulativeEnergy ? request.GridExportEnergyKWhToday : request.GridExportEnergyKWhTotal;
            existingRecord.GeneratorEnergyKWh = request.DataType == DataTypes.DailyCumulativeEnergy ? request.GeneratorEnergyKWhToday : request.GeneratorEnergyKWhTotal;
            existingRecord.BatteryDischargeEnergyKWh = request.DataType == DataTypes.DailyCumulativeEnergy ? request.BatteryDischargeEnergyKWhToday : request.BatteryDischargeEnergyKWhTotal;
            existingRecord.BatteryChargeEnergyKWh = request.DataType == DataTypes.DailyCumulativeEnergy ? request.BatteryChargeEnergyKWhToday : request.BatteryChargeEnergyKWhTotal;
            existingRecord.LoadEnergyKWh = request.DataType == DataTypes.DailyCumulativeEnergy ? request.LoadEnergyKWhToday : request.LoadEnergyKWhTotal;
            
            existingRecord.LastModified = DateTime.UtcNow;
            existingRecord.LastModifiedBy = request.CreatedBy ?? EnergyDataSources.Device;
        }
        else
        {
            // Yeni kayıt oluştur - DataType'a göre Today veya Total değerini kullan
            var energyData = new DeviceEnergyData
            {
                ThingsBoardDeviceId = request.DeviceId,
                DeviceLocalTime = localDeviceLocalTime, // Store in device local time
                DataType = request.DataType,
                MessageType = MessageTypes.Telemetry,
                PhaseType = PhaseTypes.SinglePhase,
                
                PvTotalEnergyKWh = request.DataType == DataTypes.DailyCumulativeEnergy ? request.PvEnergyKWhToday : request.PvEnergyKWhTotal,
                PvMppt1EnergyKWh = request.PvMppt1EnergyKWh,
                PvMppt2EnergyKWh = request.PvMppt2EnergyKWh,
                PvMppt3EnergyKWh = request.PvMppt3EnergyKWh,
                PvMppt4EnergyKWh = request.PvMppt4EnergyKWh,
                GridImportEnergyKWh = request.DataType == DataTypes.DailyCumulativeEnergy ? request.GridImportEnergyKWhToday : request.GridImportEnergyKWhTotal,
                GridExportEnergyKWh = request.DataType == DataTypes.DailyCumulativeEnergy ? request.GridExportEnergyKWhToday : request.GridExportEnergyKWhTotal,
                GeneratorEnergyKWh = request.DataType == DataTypes.DailyCumulativeEnergy ? request.GeneratorEnergyKWhToday : request.GeneratorEnergyKWhTotal,
                BatteryDischargeEnergyKWh = request.DataType == DataTypes.DailyCumulativeEnergy ? request.BatteryDischargeEnergyKWhToday : request.BatteryDischargeEnergyKWhTotal,
                BatteryChargeEnergyKWh = request.DataType == DataTypes.DailyCumulativeEnergy ? request.BatteryChargeEnergyKWhToday : request.BatteryChargeEnergyKWhTotal,
                LoadEnergyKWh = request.DataType == DataTypes.DailyCumulativeEnergy ? request.LoadEnergyKWhToday : request.LoadEnergyKWhTotal,
                
                Created = DateTime.UtcNow,
                CreatedBy = request.CreatedBy ?? EnergyDataSources.Device
            };

            _context.DeviceEnergyData.Add(energyData);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return existingRecord?.Id ?? 0;
    }
}
