using Microsoft.AspNetCore.Mvc;
using Hems.DeviceSimulator.Services;
using Microsoft.OpenApi.Models;

namespace Hems.DeviceSimulator.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DeviceController : ControllerBase
{
    private readonly IDeviceSimulatorService _simulatorService;
    private readonly ILogger<DeviceController> _logger;

    public DeviceController(
        IDeviceSimulatorService simulatorService,
        ILogger<DeviceController> logger)
    {
        _simulatorService = simulatorService;
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
            await _simulatorService.StartSimulationAsync();
            return Ok(new { message = "Device simulation started successfully", timestamp = DateTime.UtcNow });
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
            await _simulatorService.StopSimulationAsync();
            return Ok(new { message = "Device simulation stopped successfully", timestamp = DateTime.UtcNow });
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
            var isRunning = await _simulatorService.IsRunningAsync();
            return Ok(new 
            { 
                isRunning = isRunning,
                timestamp = DateTime.UtcNow,
                uptime = isRunning ? "Running" : "Stopped"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting simulation status");
            return StatusCode(500, new { error = "Failed to get status", details = ex.Message });
        }
    }

    /// <summary>
    /// Generates test data for a specific device component
    /// </summary>
    /// <param name="deviceId">The unique identifier of the device</param>
    /// <param name="componentId">The unique identifier of the component</param>
    /// <returns>Generated test data for the component</returns>
    [HttpGet("generate-data/{deviceId:guid}/{componentId:guid}")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(object), 404)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<IActionResult> GenerateTestData(Guid deviceId, Guid componentId)
    {
        try
        {
            var result = await _simulatorService.GenerateTestDataAsync(deviceId, componentId);
            
            if (result == null)
            {
                return NotFound(new { error = "Device or component not found", deviceId, componentId });
            }

            return Ok(new 
            { 
                DeviceId = deviceId,
                ComponentId = componentId,
                DataType = result.Value.DataType,
                Data = result.Value.Data,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating test data for device {DeviceId}, component {ComponentId}", deviceId, componentId);
            return StatusCode(500, new { error = "Failed to generate test data", details = ex.Message });
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

