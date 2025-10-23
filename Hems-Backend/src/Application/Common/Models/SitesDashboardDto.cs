namespace Hems_Backend.Application.Common.Models;

public record SitesDashboardDto
{
    public int TotalHomes { get; init; }
    public int TotalDevices { get; init; }
    public string TotalPower { get; init; } = string.Empty;
    public List<SiteWithPowerDto> SiteList { get; init; } = new();
}

public class SiteWithPowerDto : SiteDto
{
    public string Power { get; set; } = string.Empty;
}
