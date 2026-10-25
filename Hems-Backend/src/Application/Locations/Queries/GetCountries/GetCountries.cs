using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Locations.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Locations.Queries.GetCountries;

public record GetCountriesQuery : IRequest<List<CountryDto>>;

public class GetCountriesQueryHandler : IRequestHandler<GetCountriesQuery, List<CountryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetCountriesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<CountryDto>> Handle(GetCountriesQuery request, CancellationToken cancellationToken)
    {
        return await _context.Countries
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new CountryDto
            {
                Id = c.Id,
                Name = c.Name,
                Code = c.Code,
                IsActive = c.IsActive
            })
            .ToListAsync(cancellationToken);
    }
}
