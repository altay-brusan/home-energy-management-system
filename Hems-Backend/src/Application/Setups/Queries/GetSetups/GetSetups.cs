using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Setups.Queries.GetSetups;

public record GetSetupsQuery : IRequest<PaginatedList<SetupDto>>
{
    public Guid? SiteId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public class GetSetupsQueryHandler : IRequestHandler<GetSetupsQuery, PaginatedList<SetupDto>>
{
    private readonly IApplicationDbContext _context;

    public GetSetupsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<SetupDto>> Handle(GetSetupsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Setups.AsQueryable();

        if (request.SiteId.HasValue)
        {
            query = query.Where(x => x.SiteId == request.SiteId.Value);
        }

        var setups = await query
            .OrderBy(x => x.Name)
            .Select(x => new SetupDto
            {
                Id = x.Id,
                SiteId = x.SiteId,
                Name = x.Name,
                Created = x.Created.DateTime,
                CreatedBy = x.CreatedBy,
                LastModified = x.LastModified.DateTime,
                LastModifiedBy = x.LastModifiedBy
            })
            .ToListAsync(cancellationToken);

        return new PaginatedList<SetupDto>(setups, setups.Count, request.PageNumber, request.PageSize);
    }
}
