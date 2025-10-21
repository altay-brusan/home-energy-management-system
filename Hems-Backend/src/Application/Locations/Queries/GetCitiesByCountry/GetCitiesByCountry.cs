using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Locations.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Locations.Queries.GetCitiesByCountry;

public record GetCitiesByCountryQuery : IRequest<List<CityDto>>
{
    public Guid CountryId { get; init; }
}

public class GetCitiesByCountryQueryHandler : IRequestHandler<GetCitiesByCountryQuery, List<CityDto>>
{
    private readonly IApplicationDbContext _context;

    public GetCitiesByCountryQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<CityDto>> Handle(GetCitiesByCountryQuery request, CancellationToken cancellationToken)
    {
        return await _context.Cities
            .Include(c => c.Country)
            .Where(c => c.CountryId == request.CountryId && c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new CityDto
            {
                Id = c.Id,
                Name = c.Name,
                CountryId = c.CountryId,
                CountryName = c.Country.Name,
                IsActive = c.IsActive
            })
            .ToListAsync(cancellationToken);
    }
}
