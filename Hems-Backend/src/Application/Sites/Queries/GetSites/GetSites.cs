using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Mappings;
using Hems_Backend.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Sites.Queries.GetSites;

public record GetSitesQuery : IRequest<PaginatedList<SiteDto>>
{
    public string? UserId { get; init; }
    public string? UserEmail { get; init; }
    public string? UserName { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    
    // Filtreleme parametreleri
    public string? Name { get; init; }
    public string? Country { get; init; }
    public string? City { get; init; }
    public string? AddressLine { get; init; }
    public string? District { get; init; }
}

public class GetSitesQueryHandler : IRequestHandler<GetSitesQuery, PaginatedList<SiteDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUserService _userService;

    public GetSitesQueryHandler(IApplicationDbContext context, IUserService userService)
    {
        _context = context;
        _userService = userService;
    }

    public async Task<PaginatedList<SiteDto>> Handle(GetSitesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Sites.AsQueryable();

        // User filtreleme mantığı
        if (!string.IsNullOrEmpty(request.UserId))
        {
            query = query.Where(x => x.UserId == request.UserId);
        }
        else if (!string.IsNullOrEmpty(request.UserEmail))
        {
            // Email'e göre kullanıcıları bul ve site'ları filtrele
            var usersWithEmail = await _userService.GetUserIdsByEmailAsync(request.UserEmail, cancellationToken);
            query = query.Where(x => usersWithEmail.Contains(x.UserId));
        }
        else if (!string.IsNullOrEmpty(request.UserName))
        {
            // Ad-soyad'a göre kullanıcıları bul ve site'ları filtrele
            var usersWithName = await _userService.GetUserIdsByNameAsync(request.UserName, cancellationToken);
            query = query.Where(x => usersWithName.Contains(x.UserId));
        }
        
        // Only get non-deleted sites
        query = query.Where(x => !x.IsDeleted);

        // Filtreleme mantığı
        if (!string.IsNullOrEmpty(request.Name))
        {
            query = query.Where(x => x.Name.Contains(request.Name));
        }

        if (!string.IsNullOrEmpty(request.Country))
        {
            query = query.Where(x => x.Country != null && x.Country.Contains(request.Country));
        }

        if (!string.IsNullOrEmpty(request.City))
        {
            query = query.Where(x => x.City != null && x.City.Contains(request.City));
        }

        if (!string.IsNullOrEmpty(request.AddressLine))
        {
            query = query.Where(x => x.AddressLine != null && x.AddressLine.ToLower().Contains(request.AddressLine.ToLower()));
        }

        if (!string.IsNullOrEmpty(request.District))
        {
            query = query.Where(x => x.District != null && x.District.ToLower().Contains(request.District.ToLower()));
        }

        var sites = await query
            .Include(x => x.Setup!)
                .ThenInclude(s => s.SetupDevice)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var siteDtos = sites.Select(x => new SiteDto
        {
            Id = x.Id,
            UserId = x.UserId,
            Name = x.Name,
            Country = x.Country,
            City = x.City,
            District = x.District,
            AddressLine = x.AddressLine,
            Latitude = x.Latitude,
            Longitude = x.Longitude,
            Timezone = x.Timezone,
            IsDeleted = x.IsDeleted,
            HasDevice = x.Setup?.SetupDevice != null,
            Created = x.Created.DateTime,
            CreatedBy = x.CreatedBy,
            LastModified = x.LastModified.DateTime,
            LastModifiedBy = x.LastModifiedBy
        }).ToList();

        return new PaginatedList<SiteDto>(siteDtos, siteDtos.Count, request.PageNumber, request.PageSize);
    }
}

