using Hems_Backend.Application.Common.Models;

namespace Hems_Backend.Application.Common.Models;

public class UserStatusDto
{
    public UserStatusType Status { get; set; }
    public string RedirectTo { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public UserStatusDataDto? Data { get; set; }
}

public class UserStatusDataDto
{
    public Guid? SiteId { get; set; }
    public string? SiteName { get; set; }
    public Guid? SetupId { get; set; }
    public string? SetupName { get; set; }
    public Guid? DeviceId { get; set; }
    public string? DeviceName { get; set; }
}

public enum UserStatusType
{
    Complete = 1,           // Her şey tamam
    NeedsSite = 2,          // Site oluşturulmalı
    NeedsSetup = 3,         // Setup oluşturulmalı (device ile birlikte)
    Error = 4               // Hata durumu
}
