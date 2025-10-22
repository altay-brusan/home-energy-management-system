using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Mappings;
using Hems_Backend.Application.ParameterGroups.Queries.GetParameterGroups;
using MediatR;

namespace Hems_Backend.Application.ParameterGroups.Queries.GetParameterGroup;

public record GetParameterGroupQuery : IRequest<ParameterGroupDto?>
{
    public Guid Id { get; init; }
    public bool IncludeChildren { get; init; } = false;
}

public class GetParameterGroupQueryHandler : IRequestHandler<GetParameterGroupQuery, ParameterGroupDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetParameterGroupQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ParameterGroupDto?> Handle(GetParameterGroupQuery request, CancellationToken cancellationToken)
    {
        var query = _context.ParameterGroups
            .Where(pg => pg.Id == request.Id);

        if (request.IncludeChildren)
        {
            query = query.Include(pg => pg.Children)
                        .Include(pg => pg.Parameters);
        }

        return await query
            .ProjectTo<ParameterGroupDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
