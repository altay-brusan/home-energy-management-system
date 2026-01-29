using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Infrastructure.Services;

/// <summary>
/// Background service that periodically checks ALM service health
/// and monitors active prediction jobs.
/// </summary>
public class AlmHealthCheckService : BackgroundService
{
    private readonly IAlmService _almService;
    private readonly IHubContext<AlmNotificationHub, IAlmNotificationHubClient> _hubContext;
    private readonly ILogger<AlmHealthCheckService> _logger;
    private readonly TimeSpan _healthCheckInterval;
    private bool _lastHealthStatus = true;

    public AlmHealthCheckService(
        IAlmService almService,
        IHubContext<AlmNotificationHub, IAlmNotificationHubClient> hubContext,
        IConfiguration configuration,
        ILogger<AlmHealthCheckService> logger)
    {
        _almService = almService;
        _hubContext = hubContext;
        _logger = logger;
        var intervalSeconds = configuration.GetValue("Alm:HealthCheckIntervalSeconds", 30);
        _healthCheckInterval = TimeSpan.FromSeconds(intervalSeconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ALM Health Check Service started. Interval: {Interval}s", _healthCheckInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var isHealthy = await _almService.IsHealthyAsync(stoppingToken);

                // Only notify if health status changed
                if (isHealthy != _lastHealthStatus)
                {
                    _logger.LogInformation("ALM service health changed: {Status}", isHealthy ? "Healthy" : "Unhealthy");
                    _lastHealthStatus = isHealthy;

                    await _hubContext.Clients.All.AlmHealthStatus(new AlmHealthStatusNotification
                    {
                        IsHealthy = isHealthy,
                        Timestamp = DateTimeOffset.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during ALM health check");
            }

            await Task.Delay(_healthCheckInterval, stoppingToken);
        }
    }
}
