using Hems_Backend.Application.Common.Models;

namespace Hems_Backend.Application.Sites.Queries.GetSites;

public record GetSitesResponseDto
{
    public int TotalSite { get; init; }
    public int TotalDevice { get; init; }
    public PaginatedList<SiteDto> Data { get; init; } = null!;
}

