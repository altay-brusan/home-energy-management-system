using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Mappings;
using Hems_Backend.Application.Parameters.Queries.GetParameters;
using MediatR;

namespace Hems_Backend.Application.Parameters.Queries.GetParameter;

public record GetParameterQuery : IRequest<ParameterDto?>
{
    public Guid Id { get; init; }
    public bool IncludeGroup { get; init; } = false;
}

public class GetParameterQueryHandler : IRequestHandler<GetParameterQuery, ParameterDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetParameterQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ParameterDto?> Handle(GetParameterQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Parameters
            .Where(p => p.Id == request.Id);

        if (request.IncludeGroup)
        {
            query = query.Include(p => p.ParameterGroup);
        }

        return await query
            .ProjectTo<ParameterDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
