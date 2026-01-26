using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Locations.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Locations.Queries.GetCountryCities;

public record GetCountryCitiesQuery : IRequest<List<CityDto>>
{
    public Guid CountryId { get; init; }
}

public class GetCountryCitiesQueryHandler : IRequestHandler<GetCountryCitiesQuery, List<CityDto>>
{
    private readonly IApplicationDbContext _context;

    public GetCountryCitiesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<CityDto>> Handle(GetCountryCitiesQuery request, CancellationToken cancellationToken)
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
