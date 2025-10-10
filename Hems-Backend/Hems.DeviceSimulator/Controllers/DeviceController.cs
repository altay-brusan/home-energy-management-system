using Microsoft.AspNetCore.Mvc;
using Hems.DeviceSimulator.Services;
using Microsoft.OpenApi.Models;

namespace Hems.DeviceSimulator.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DeviceController : ControllerBase
{
    private readonly IDeviceSimulationJobService _jobService;
    private readonly IEnergyDataJobService _energyJobService;
    private readonly ILogger<DeviceController> _logger;

    public DeviceController(
        IDeviceSimulationJobService jobService,
        IEnergyDataJobService energyJobService,
        ILogger<DeviceController> logger)
    {
        _jobService = jobService;
        _energyJobService = energyJobService;
        _logger = logger;
    }

    /// <summary>
    /// Starts the device simulation by fetching devices from database and generating data
    /// </summary>
    /// <returns>Success message with timestamp</returns>
    [HttpPost("start")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<IActionResult> StartSimulation()
    {
        try
        {
            // Hem device simulation hem de energy data collection'ı başlat
            await _jobService.StartSimulationAsync();
            await _energyJobService.StartEnergyDataCollectionAsync();
            
            return Ok(new { 
                message = "Device simulation and energy data collection started successfully with Hangfire", 
                deviceSimulation = "Started",
                energyDataCollection = "Started",
                timestamp = DateTime.UtcNow 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting simulation");
            return StatusCode(500, new { error = "Failed to start simulation", details = ex.Message });
        }
    }

    /// <summary>
    /// Stops the device simulation
    /// </summary>
    /// <returns>Success message with timestamp</returns>
    [HttpPost("stop")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<IActionResult> StopSimulation()
    {
        try
        {
            // Hem device simulation hem de energy data collection'ı durdur
            await _jobService.StopSimulationAsync();
            await _energyJobService.StopEnergyDataCollectionAsync();
            
            return Ok(new { 
                message = "Device simulation and energy data collection stopped successfully", 
                deviceSimulation = "Stopped",
                energyDataCollection = "Stopped",
                timestamp = DateTime.UtcNow 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping simulation");
            return StatusCode(500, new { error = "Failed to stop simulation", details = ex.Message });
        }
    }

    /// <summary>
    /// Gets the current simulation status
    /// </summary>
    /// <returns>Current simulation status with timestamp</returns>
    [HttpGet("status")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<IActionResult> GetStatus()
    {
        try
        {
            var isRunning = await _jobService.IsRunningAsync();
            return Ok(new 
            { 
                isRunning = isRunning,
                timestamp = DateTime.UtcNow,
                uptime = isRunning ? "Running with Hangfire" : "Stopped",
                dashboardUrl = "/hangfire"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting simulation status");
            return StatusCode(500, new { error = "Failed to get status", details = ex.Message });
        }
    }


    /// <summary>
    /// Schedules a specific device simulation with custom interval
    /// </summary>
    /// <param name="deviceId">The unique identifier of the device</param>
    /// <param name="intervalSeconds">Interval in seconds between simulations</param>
    /// <returns>Success message</returns>
    [HttpPost("schedule/{deviceId:guid}")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<IActionResult> ScheduleDevice(Guid deviceId, [FromQuery] int intervalSeconds = 30)
    {
        try
        {
            await _jobService.ScheduleDeviceSimulationAsync(deviceId, intervalSeconds);
            return Ok(new 
            { 
                message = $"Device {deviceId} scheduled successfully", 
                deviceId = deviceId,
                intervalSeconds = intervalSeconds,
                timestamp = DateTime.UtcNow 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling device {DeviceId}", deviceId);
            return StatusCode(500, new { error = "Failed to schedule device", details = ex.Message });
        }
    }

    /// <summary>
    /// Unschedules a specific device simulation
    /// </summary>
    /// <param name="deviceId">The unique identifier of the device</param>
    /// <returns>Success message</returns>
    [HttpPost("unschedule/{deviceId:guid}")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<IActionResult> UnscheduleDevice(Guid deviceId)
    {
        try
        {
            await _jobService.UnscheduleDeviceSimulationAsync(deviceId);
            return Ok(new 
            { 
                message = $"Device {deviceId} unscheduled successfully", 
                deviceId = deviceId,
                timestamp = DateTime.UtcNow 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unscheduling device {DeviceId}", deviceId);
            return StatusCode(500, new { error = "Failed to unschedule device", details = ex.Message });
        }
    }

    /// <summary>
    /// Starts the energy data collection (hourly cumulative + midnight lifetime data)
    /// </summary>
    /// <returns>Success message with timestamp</returns>
    [HttpPost("energy/start")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<IActionResult> StartEnergyDataCollection()
    {
        try
        {
            await _energyJobService.StartEnergyDataCollectionAsync();
            return Ok(new { message = "Energy data collection started successfully with Hangfire", timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting energy data collection");
            return StatusCode(500, new { error = "Failed to start energy data collection", details = ex.Message });
        }
    }

    /// <summary>
    /// Stops the energy data collection
    /// </summary>
    /// <returns>Success message with timestamp</returns>
    [HttpPost("energy/stop")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<IActionResult> StopEnergyDataCollection()
    {
        try
        {
            await _energyJobService.StopEnergyDataCollectionAsync();
            return Ok(new { message = "Energy data collection stopped successfully", timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping energy data collection");
            return StatusCode(500, new { error = "Failed to stop energy data collection", details = ex.Message });
        }
    }
}

/// <summary>
/// Request model for sending test data to a device
/// </summary>
public class SendTestDataRequest
{
    /// <summary>
    /// The unique identifier of the device to send test data to
    /// </summary>
    /// <example>123e4567-e89b-12d3-a456-426614174000</example>
    public Guid DeviceId { get; set; }
    
    /// <summary>
    /// Optional test value (defaults to 25.5 if not provided)
    /// </summary>
    /// <example>25.5</example>
    public double? Value { get; set; }
    
    /// <summary>
    /// Optional unit for the test value (defaults to "°C" if not provided)
    /// </summary>
    /// <example>°C</example>
    public string? Unit { get; set; }
}

