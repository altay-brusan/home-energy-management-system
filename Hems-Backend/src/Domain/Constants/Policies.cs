namespace Hems_Backend.Domain.Constants;

public abstract class Policies
{
    public const string CanPurge = nameof(CanPurge);
    public const string AdminOnly = nameof(AdminOnly);
    public const string UserManagement = nameof(UserManagement);
    
    // Device Management Policies
    public const string DeviceManagement = nameof(DeviceManagement);
    public const string DeviceReadOnly = nameof(DeviceReadOnly);
    
    // Site Management Policies  
    public const string SiteManagement = nameof(SiteManagement);
    
    // Setup Management Policies
    public const string SetupManagement = nameof(SetupManagement);
    
    // Dashboard Access Policies
    public const string DashboardAccess = nameof(DashboardAccess);
    
    // Device Data Policies
    public const string DeviceOnly = nameof(DeviceOnly);
}