using Hangfire;
using Hems_Backend.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Infrastructure.Services;

/// <summary>
/// Background service that registers the energy data collection Hangfire recurring job.
/// The job runs every 15 minutes and processes all devices in their local collection window.
/// </summary>
public class EnergyDataCollectionBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRecurringJobManager _recurringJobManager;
    private readonly ILogger<EnergyDataCollectionBackgroundService> _logger;

    private const string JobId = "energy-data-collection";
    private const string CronExpression = "*/15 * * * *"; // Every 15 minutes

    public EnergyDataCollectionBackgroundService(
        IServiceScopeFactory scopeFactory,
        IRecurringJobManager recurringJobManager,
        ILogger<EnergyDataCollectionBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _recurringJobManager = recurringJobManager;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Registering Energy Data Collection recurring job with cron: {Cron}",
            CronExpression);

        // Schedule the recurring job
        _recurringJobManager.AddOrUpdate(
            JobId,
            () => ExecuteCollectionJob(),
            CronExpression,
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });

        _logger.LogInformation("Energy Data Collection job registered successfully");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Job entry point called by Hangfire.
    /// Creates a scope and invokes the energy collection service.
    /// </summary>
    [AutomaticRetry(Attempts = 0)] // Don't retry - job runs every 15 min anyway
    [DisableConcurrentExecution(timeoutInSeconds: 300)] // Prevent overlapping executions
    public async Task ExecuteCollectionJob()
    {
        _logger.LogInformation("Energy data collection job starting");

        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IEnergyDataCollectionService>();

        try
        {
            await service.ProcessEnergyDataCollectionAsync();
            _logger.LogInformation("Energy data collection job completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Energy data collection job failed");
            throw; // Re-throw to mark job as failed in Hangfire dashboard
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Energy Data Collection background service stopping");

        // Optionally remove the recurring job when the service stops
        // _recurringJobManager.RemoveIfExists(JobId);

        await base.StopAsync(cancellationToken);
    }
}
