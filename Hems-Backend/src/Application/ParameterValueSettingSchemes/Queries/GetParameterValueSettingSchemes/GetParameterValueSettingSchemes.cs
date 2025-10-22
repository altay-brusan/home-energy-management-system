using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Mappings;
using MediatR;

namespace Hems_Backend.Application.ParameterValueSettingSchemes.Queries.GetParameterValueSettingSchemes;

public record GetParameterValueSettingSchemesQuery : IRequest<ParameterValueSettingSchemesVm>
{
    public string? ModelNumber { get; init; }
}

public class GetParameterValueSettingSchemesQueryHandler : IRequestHandler<GetParameterValueSettingSchemesQuery, ParameterValueSettingSchemesVm>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetParameterValueSettingSchemesQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ParameterValueSettingSchemesVm> Handle(GetParameterValueSettingSchemesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.ParameterValueSettingSchemes.AsQueryable();

        if (!string.IsNullOrEmpty(request.ModelNumber))
        {
            query = query.Where(pvss => pvss.ModelNumber == request.ModelNumber);
        }

        var schemes = await query
            .OrderBy(pvss => pvss.SortOrder)
            .ProjectTo<ParameterValueSettingSchemeDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return new ParameterValueSettingSchemesVm
        {
            ParameterValueSettingSchemes = schemes
        };
    }
}
