using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Mappings;
using MediatR;

namespace Hems_Backend.Application.Parameters.Queries.GetParameters;

public record GetParametersQuery : IRequest<ParametersVm>
{
    public Guid? GroupId { get; init; }
}

public class GetParametersQueryHandler : IRequestHandler<GetParametersQuery, ParametersVm>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetParametersQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ParametersVm> Handle(GetParametersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Parameters.AsQueryable();

        if (request.GroupId.HasValue)
        {
            query = query.Where(p => p.ParameterGroupId == request.GroupId.Value);
        }

        var parameters = await query
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .ProjectTo<ParameterDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return new ParametersVm
        {
            Parameters = parameters
        };
    }
}
