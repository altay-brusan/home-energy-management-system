using Hems_Backend.Domain.Enums;

namespace Hems_Backend.Application.Common.Models;

public class UserSiteDto
{
    public Guid Id { get; init; }
    public string UserId { get; init; } = string.Empty;
    public Guid SiteId { get; init; }
    public string SiteName { get; init; } = string.Empty;
    public UserSiteRole Role { get; init; }
}

public class UserHomeSitesVm
{
    public List<UserSiteDto> MyHomes { get; init; } = new();
    public List<UserSiteDto> SharedHomes { get; init; } = new();
}

