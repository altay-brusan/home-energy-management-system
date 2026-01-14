using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.Devices.Queries.CheckDeviceAccess;
using Hems_Backend.Domain.Entities;
using Hems_Backend.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Hems_Backend.Application.Devices.Queries.GetComponentLiveData;

public class GetComponentLiveDataQueryHandler : IRequestHandler<GetComponentLiveDataQuery, GetComponentLiveDataResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ISender _sender;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IDemoDataService _demoDataService;

    public GetComponentLiveDataQueryHandler(
        IApplicationDbContext context,
        ISender sender,
        HttpClient httpClient,
        IConfiguration configuration,
        IDemoDataService demoDataService)
    {
        _context = context;
        _sender = sender;
        _httpClient = httpClient;
        _configuration = configuration;
        _demoDataService = demoDataService;
    }

    public async Task<GetComponentLiveDataResponse> Handle(GetComponentLiveDataQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Device access kontrolü
            var hasAccess = await _sender.Send(new CheckDeviceAccessQuery { DeviceId = request.DeviceId }, cancellationToken);
            if (!hasAccess)
            {
                return new GetComponentLiveDataResponse
                {
                    Success = false,
                    Message = "Device access denied"
                };
            }

            // 2. Component varlık kontrolü
            var component = await _context.DeviceComponents
                .FirstOrDefaultAsync(dc => dc.Id == request.ComponentId && 
                                          dc.DeviceId == request.DeviceId && 
                                          !dc.IsDeleted, cancellationToken);

            if (component == null)
            {
                return new GetComponentLiveDataResponse
                {
                    Success = false,
                    Message = "Component not found"
                };
            }

            // 3. Check if demo device - return simulated component data
            var isDemoDevice = await _demoDataService.IsDemoDeviceAsync(request.DeviceId, cancellationToken);
            if (isDemoDevice)
            {
                return GetSimulatedComponentLiveData(component.DeviceType, component.SerialNumber);
            }

            // 4. DeviceSimulator'a istek at
            var deviceSimulatorUrl = _configuration["DeviceSimulator:BaseUrl"] ?? "https://localhost:5002";
            var endpoint = $"{deviceSimulatorUrl}/api/Device/takeLiveData";
            
            var requestBody = new
            {
                DeviceId = request.DeviceId,
                ComponentId = request.ComponentId,
                ComponentType = component.DeviceType.ToString(),
                SerialNumber = component.SerialNumber
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                return new GetComponentLiveDataResponse
                {
                    Success = false,
                    Message = $"DeviceSimulator request failed: {response.StatusCode}"
                };
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var liveData = JsonSerializer.Deserialize<JsonElement>(responseContent);

            // 5. DeviceData tablosuna kaydet
            var dataType = "";
            switch (component.DeviceType)
            {
               
                case Domain.Enums.DeviceType.Invertor:
                    dataType = "InverterData";
                    break;
                case Domain.Enums.DeviceType.BMS:
                    dataType = "BatteryClusterData";
                    break;
                case Domain.Enums.DeviceType.Battery:
                    dataType = "BatteryData";
                    break;
                default:
                    break;
            }
            var deviceData = new Hems_Backend.Domain.Entities.DeviceData
            {
                ThingsBoardDeviceId = request.DeviceId,
                DeviceLocalTime = DateTime.UtcNow,
                DataType = dataType,
                Data = JsonDocument.Parse(responseContent),
                Created = DateTime.UtcNow,
            };

            _context.DeviceData.Add(deviceData);
            await _context.SaveChangesAsync(cancellationToken);

            // 6. JSON data'yı döndür
            return new GetComponentLiveDataResponse
            {
                Success = true,
                Message = "Live data retrieved successfully",
                Data = liveData
            };
        }
        catch (Exception ex)
        {
            return new GetComponentLiveDataResponse
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Returns simulated component live data for demo devices
    /// Data varies by component type (Inverter, BMS, Battery)
    /// </summary>
    private static GetComponentLiveDataResponse GetSimulatedComponentLiveData(DeviceType componentType, string? serialNumber)
    {
        var now = DateTime.UtcNow;
        var hour = now.Hour;
        var random = new Random(now.Minute);

        object simulatedData = componentType switch
        {
            DeviceType.Invertor => new
            {
                timestampUtc = now.ToString("o"),
                serialNumber = serialNumber ?? "DEMO-INV-001",
                status = "Running",
                acOutputPower = GetInverterPowerByHour(hour, random),
                acOutputVoltage = 230.0 + random.NextDouble() * 5,
                acOutputFrequency = 50.0 + (random.NextDouble() - 0.5) * 0.2,
                dcInputVoltage = 380.0 + random.NextDouble() * 20,
                dcInputCurrent = 8.0 + random.NextDouble() * 4,
                temperature = 35.0 + random.NextDouble() * 10,
                efficiency = 95.0 + random.NextDouble() * 3,
                totalEnergyToday = GetDailyEnergyByHour(hour),
                operatingMode = "Normal"
            },
            DeviceType.BMS => new
            {
                timestampUtc = now.ToString("o"),
                serialNumber = serialNumber ?? "DEMO-BMS-001",
                status = "Normal",
                socPercent = GetSocByHour(hour, random),
                sohPercent = 98.5,
                totalVoltage = 51.2 + random.NextDouble() * 2,
                totalCurrent = GetBatteryCurrentByHour(hour, random),
                temperature = 25.0 + random.NextDouble() * 5,
                cellVoltageMax = 3.42 + random.NextDouble() * 0.05,
                cellVoltageMin = 3.38 + random.NextDouble() * 0.05,
                cellTempMax = 28.0 + random.NextDouble() * 3,
                cellTempMin = 24.0 + random.NextDouble() * 3,
                cycleCount = 245,
                chargeEnabled = true,
                dischargeEnabled = true
            },
            DeviceType.Battery => new
            {
                timestampUtc = now.ToString("o"),
                serialNumber = serialNumber ?? "DEMO-BAT-001",
                status = "Normal",
                socPercent = GetSocByHour(hour, random),
                voltage = 51.5 + random.NextDouble() * 2,
                current = GetBatteryCurrentByHour(hour, random),
                power = Math.Abs(GetBatteryCurrentByHour(hour, random) * 51.5),
                temperature = 26.0 + random.NextDouble() * 4,
                capacityKwh = 10.24,
                remainingCapacityKwh = GetSocByHour(hour, random) / 100.0 * 10.24
            },
            _ => new
            {
                timestampUtc = now.ToString("o"),
                serialNumber = serialNumber ?? "DEMO-COMPONENT",
                status = "Normal",
                message = "Simulated demo data"
            }
        };

        var jsonString = JsonSerializer.Serialize(simulatedData);
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        return new GetComponentLiveDataResponse
        {
            Success = true,
            Message = "Live data retrieved successfully (simulated)",
            Data = jsonElement
        };
    }

    private static double GetInverterPowerByHour(int hour, Random random)
    {
        // PV power follows solar pattern
        return hour switch
        {
            >= 6 and < 8 => 500 + random.NextDouble() * 500,
            >= 8 and < 10 => 1500 + random.NextDouble() * 800,
            >= 10 and < 14 => 3500 + random.NextDouble() * 1000,
            >= 14 and < 16 => 2800 + random.NextDouble() * 800,
            >= 16 and < 18 => 1200 + random.NextDouble() * 600,
            >= 18 and < 20 => 300 + random.NextDouble() * 300,
            _ => 0
        };
    }

    private static double GetDailyEnergyByHour(int hour)
    {
        // Cumulative energy throughout the day
        return hour switch
        {
            < 6 => 0,
            >= 6 and < 9 => (hour - 6) * 1.5,
            >= 9 and < 15 => 4.5 + (hour - 9) * 3.5,
            >= 15 and < 18 => 25.5 + (hour - 15) * 2.0,
            _ => 31.5 + (hour - 18) * 0.3
        };
    }

    private static double GetSocByHour(int hour, Random random)
    {
        return hour switch
        {
            >= 0 and < 6 => 45 + random.NextDouble() * 10,
            >= 6 and < 9 => 35 + random.NextDouble() * 10,
            >= 9 and < 12 => 55 + random.NextDouble() * 20,
            >= 12 and < 15 => 80 + random.NextDouble() * 15,
            >= 15 and < 18 => 85 + random.NextDouble() * 15,
            >= 18 and < 21 => 60 + random.NextDouble() * 20,
            _ => 50 + random.NextDouble() * 15
        };
    }

    private static double GetBatteryCurrentByHour(int hour, Random random)
    {
        // Positive = charging, negative = discharging
        return hour switch
        {
            >= 9 and < 15 => 15 + random.NextDouble() * 10, // Charging during solar peak
            >= 18 and < 22 => -(10 + random.NextDouble() * 8), // Discharging evening
            >= 6 and < 9 => -(5 + random.NextDouble() * 5), // Light discharge morning
            _ => random.NextDouble() * 2 - 1 // Near idle at night
        };
    }
}
