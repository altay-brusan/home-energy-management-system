using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Mappings;
using Hems_Backend.Application.Common.Models;
using MediatR;

namespace Hems_Backend.Application.ParameterGroups.Queries.GetParameterGroups;

public record GetParameterGroupsQuery : IRequest<ParameterGroupsVm>;

public class GetParameterGroupsQueryHandler : IRequestHandler<GetParameterGroupsQuery, ParameterGroupsVm>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetParameterGroupsQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ParameterGroupsVm> Handle(GetParameterGroupsQuery request, CancellationToken cancellationToken)
    {
        var parameterGroups = await _context.ParameterGroups
            .OrderBy(pg => pg.SortOrder)
            .ThenBy(pg => pg.Name)
            .ProjectTo<ParameterGroupDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        // Hierarchical yapı oluştur
        var hierarchicalGroups = BuildHierarchy(parameterGroups);

        return new ParameterGroupsVm
        {
            ParameterGroups = hierarchicalGroups
        };
    }

    private static List<ParameterGroupDto> BuildHierarchy(List<ParameterGroupDto> allGroups)
    {
        var lookup = allGroups.ToLookup(g => g.ParentId);
        
        foreach (var group in allGroups)
        {
            group.Children = lookup[group.Id].ToList();
        }

        return lookup[null].ToList();
    }
}
