using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.Devices.Queries.CheckDeviceAccess;
using Hems_Backend.Domain.Entities;
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

    public GetComponentLiveDataQueryHandler(
        IApplicationDbContext context, 
        ISender sender, 
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _context = context;
        _sender = sender;
        _httpClient = httpClient;
        _configuration = configuration;
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

            // 3. DeviceSimulator'a istek at
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

            // 4. DeviceData tablosuna kaydet
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
                DeviceId = request.DeviceId,
                RequestDate = DateTime.UtcNow,
                DataType = dataType,
                Data = JsonDocument.Parse(responseContent),
                Created = DateTime.UtcNow,
            };

            _context.DeviceData.Add(deviceData);
            await _context.SaveChangesAsync(cancellationToken);

            // 5. JSON data'yı döndür
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
}
