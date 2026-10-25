namespace Hems_Backend.Application.Locations.Common.Models;

public class CountryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

public class CityDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public Guid CountryId { get; init; }
    public string CountryName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}
