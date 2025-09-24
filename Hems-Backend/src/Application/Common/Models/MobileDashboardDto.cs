using Hems_Backend.Domain.Enums;
using System.Text.Json;

namespace Hems_Backend.Application.Common.Models;

public class MobileDashboardDto
{
    public Guid SiteId { get; set; }
    public string SiteName { get; set; } = string.Empty;
    public Guid SetupId { get; set; }
    public string SetupName { get; set; } = string.Empty;
    public DeviceOverviewDto Device { get; set; } = null!;
    public List<ExternalDeviceOverviewDto> ExternalDevices { get; set; } = new();
    public List<AlertDto> ActiveAlerts { get; set; } = new();
    public SystemStatusDto SystemStatus { get; set; } = null!;
}

public class DeviceOverviewDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string ModelNumber { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string QRCode { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public DateTime? LastSeen { get; set; }
    public JsonDocument Specifications { get; set; } = JsonDocument.Parse("{}");
    public List<DeviceComponentOverviewDto> Components { get; set; } = new();
    public WarrantyInfoDto Warranty { get; set; } = null!;
}

public class DeviceComponentOverviewDto
{
    public Guid Id { get; set; }
    public DeviceType DeviceType { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string ModelNumber { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public JsonDocument Specifications { get; set; } = JsonDocument.Parse("{}");
    public bool IsActive { get; set; }
    public WarrantyInfoDto Warranty { get; set; } = null!;
}

public class ExternalDeviceOverviewDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public JsonDocument Specifications { get; set; } = JsonDocument.Parse("{}");
    public bool IsActive { get; set; }
    public DateTime Created { get; set; }
}

public class AlertDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public AlertType Type { get; set; }
    public AlertPriority Priority { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
}

public class SystemStatusDto
{
    public string OverallStatus { get; set; } = string.Empty; // Normal, Warning, Error
    public int BatteryLevel { get; set; } // 0-100
    public double PowerGeneration { get; set; } // W
    public double PowerConsumption { get; set; } // W
    public double GridImport { get; set; } // W
    public double GridExport { get; set; } // W
    public string OperatingMode { get; set; } = string.Empty; // Self-Consumption, Grid-Tie, Backup
    public List<double> BatteryTemperatures { get; set; } = new(); // Celsius
    public double AmbientTemperature { get; set; } // Celsius
    public string FanState { get; set; } = string.Empty; // Off, Low, Medium, High
    public DateTime LastUpdate { get; set; }
}

public class WarrantyInfoDto
{
    public int? WarrantyPeriod { get; set; }
    public WarrantyUnit? WarrantyUnit { get; set; }
    public DateTime? WarrantyStartDate { get; set; }
    public DateTime? WarrantyEndDate { get; set; }
    public int DaysRemaining { get; set; }
    public bool IsExpired { get; set; }
}

public enum AlertType
{
    Info = 1,
    Warning = 2,
    Error = 3,
    Maintenance = 4
}

public enum AlertPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}
