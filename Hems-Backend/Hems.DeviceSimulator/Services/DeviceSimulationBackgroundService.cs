using Hems.DeviceSimulator.Services;

namespace Hems.DeviceSimulator.Services;

public class DeviceSimulationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DeviceSimulationBackgroundService> _logger;
    private readonly IConfiguration _configuration;

    public DeviceSimulationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<DeviceSimulationBackgroundService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Başlangıçta kısa bir bekleme
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        // Otomatik başlatma ayarı kontrol et
        var autoStart = _configuration.GetValue<bool>("DeviceSimulator:AutoStart", false);
        
        if (!autoStart)
        {
            _logger.LogInformation("🚀 Device Simulator Background Service started (manual start required)");
            return;
        }

        _logger.LogInformation("🚀 Device Simulator Background Service started (auto-start enabled)");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var simulatorService = scope.ServiceProvider.GetRequiredService<IDeviceSimulatorService>();
            
            // Otomatik olarak simülasyonu başlat
            await simulatorService.StartSimulationAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Device Simulation Background Service");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🛑 Device Simulator Background Service stopping...");
        
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var simulatorService = scope.ServiceProvider.GetRequiredService<IDeviceSimulatorService>();
            
            await simulatorService.StopSimulationAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping Device Simulation Background Service");
        }
        
        await base.StopAsync(cancellationToken);
    }
}



