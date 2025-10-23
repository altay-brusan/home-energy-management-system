using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Mappings;
using Hems_Backend.Domain.Entities;
using MediatR;

namespace Hems_Backend.Application.ParameterValueSettingSchemes.Queries.GetParameterValueSettingSchemesTree;

public record GetParameterValueSettingSchemesTreeQuery : IRequest<ParameterValueSettingSchemesTreeVm>
{
    public string? ModelNumber { get; init; }
}

public class GetParameterValueSettingSchemesTreeQueryHandler : IRequestHandler<GetParameterValueSettingSchemesTreeQuery, ParameterValueSettingSchemesTreeVm>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetParameterValueSettingSchemesTreeQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ParameterValueSettingSchemesTreeVm> Handle(GetParameterValueSettingSchemesTreeQuery request, CancellationToken cancellationToken)
    {
        // 1. ParameterValueSettingSchemes'ları çek
        var query = _context.ParameterValueSettingSchemes.AsQueryable();

        if (!string.IsNullOrEmpty(request.ModelNumber))
        {
            query = query.Where(pvss => pvss.ModelNumber == request.ModelNumber);
        }

        var parameterValueSettingSchemes = await query
            .Include(pvss => pvss.Parameter)
            .ToListAsync(cancellationToken);

        if (!parameterValueSettingSchemes.Any())
        {
            return new ParameterValueSettingSchemesTreeVm
            {
                ModelNumber = request.ModelNumber,
                ParameterGroups = new List<ParameterGroupTreeDto>()
            };
        }

        // 2. Bu parameter'ların ID'lerini al
        var parameterIds = parameterValueSettingSchemes.Select(pvss => pvss.ParameterId).ToList();

        // 3. Parameter'ları ParameterGroup ile birlikte çek
        var parameters = await _context.Parameters
            .Where(p => parameterIds.Contains(p.Id))
            .Include(p => p.ParameterGroup)
            .ToListAsync(cancellationToken);

        // 4. Bu parameter'ların ParameterGroup ID'lerini al
        var parameterGroupIds = parameters
            .Where(p => p.ParameterGroup != null)
            .Select(p => p.ParameterGroup!.Id)
            .Distinct()
            .ToList();

        // 5. Tüm ParameterGroup'ları (parent'lar dahil) çek
        var allParameterGroups = await GetAllParameterGroupsWithParents(parameterGroupIds, cancellationToken);

        // 6. Hierarchical yapı oluştur
        var tree = BuildHierarchicalTree(parameterValueSettingSchemes, parameters, allParameterGroups);

        return new ParameterValueSettingSchemesTreeVm
        {
            ModelNumber = request.ModelNumber,
            ParameterGroups = tree
        };
    }

    private async Task<List<ParameterGroup>> GetAllParameterGroupsWithParents(List<Guid> parameterGroupIds, CancellationToken cancellationToken)
    {
        var allGroups = new List<ParameterGroup>();
        var processedIds = new HashSet<Guid>();

        // İlk seviye grupları çek
        var currentLevelGroups = await _context.ParameterGroups
            .Where(pg => parameterGroupIds.Contains(pg.Id))
            .ToListAsync(cancellationToken);

        allGroups.AddRange(currentLevelGroups);
        currentLevelGroups.ForEach(g => processedIds.Add(g.Id));

        // Parent'ları recursive olarak çek
        var parentIds = currentLevelGroups
            .Where(g => g.ParentId.HasValue)
            .Select(g => g.ParentId!.Value)
            .Where(id => !processedIds.Contains(id))
            .ToList();

        while (parentIds.Any())
        {
            var parentGroups = await _context.ParameterGroups
                .Where(pg => parentIds.Contains(pg.Id))
                .ToListAsync(cancellationToken);

            allGroups.AddRange(parentGroups);
            parentGroups.ForEach(g => processedIds.Add(g.Id));

            parentIds = parentGroups
                .Where(g => g.ParentId.HasValue)
                .Select(g => g.ParentId!.Value)
                .Where(id => !processedIds.Contains(id))
                .ToList();
        }

        return allGroups;
    }

    private static List<ParameterGroupTreeDto> BuildHierarchicalTree(
        List<ParameterValueSettingScheme> parameterValueSettingSchemes,
        List<Parameter> parameters,
        List<ParameterGroup> allParameterGroups)
    {
        // 1. Tüm ParameterGroup'ları DTO'ya çevir
        var allGroups = new Dictionary<Guid, ParameterGroupTreeDto>();
        
        foreach (var group in allParameterGroups)
        {
            allGroups[group.Id] = new ParameterGroupTreeDto
            {
                Id = group.Id,
                Name = group.Name,
                Description = group.Description,
                Version = group.Version,
                SortOrder = group.SortOrder,
                ParentId = group.ParentId,
                Children = new List<ParameterGroupTreeDto>(),
                Parameters = new List<ParameterTreeDto>()
            };
        }

        // 2. Hierarchical yapı oluştur (parent-child ilişkileri)
        var rootGroups = new List<ParameterGroupTreeDto>();
        
        foreach (var group in allGroups.Values)
        {
            if (group.ParentId == null)
            {
                rootGroups.Add(group);
            }
            else if (allGroups.ContainsKey(group.ParentId.Value))
            {
                allGroups[group.ParentId.Value].Children.Add(group);
            }
        }

        // 3. Parameter'ları ilgili gruplara ekle
        var parameterLookup = parameters.ToDictionary(p => p.Id, p => p);
        
        foreach (var pvss in parameterValueSettingSchemes)
        {
            if (parameterLookup.ContainsKey(pvss.ParameterId))
            {
                var parameter = parameterLookup[pvss.ParameterId];
                
                if (parameter.ParameterGroup != null && allGroups.ContainsKey(parameter.ParameterGroup.Id))
                {
                    var parameterDto = new ParameterTreeDto
                    {
                        Id = parameter.Id,
                        Name = parameter.Name,
                        Description = parameter.Description,
                        ElementType = parameter.ElementType,
                        RoleType = parameter.RoleType,
                        Unit = parameter.Unit,
                        DefaultValue = parameter.DefaultValue,
                        SortOrder = parameter.SortOrder,
                        ParameterValueSettingScheme = new ParameterValueSettingSchemeTreeDto
                        {
                            Id = pvss.Id,
                            Value = pvss.Value,
                            SortOrder = pvss.SortOrder,
                            ModelNumber = pvss.ModelNumber,
                            Created = pvss.Created,
                            CreatedBy = pvss.CreatedBy,
                            LastModified = pvss.LastModified,
                            LastModifiedBy = pvss.LastModifiedBy
                        }
                    };

                    allGroups[parameter.ParameterGroup.Id].Parameters.Add(parameterDto);
                }
            }
        }

        // 4. Sort order'a göre sırala
        SortTree(rootGroups);

        return rootGroups;
    }

    private static void SortTree(List<ParameterGroupTreeDto> groups)
    {
        foreach (var group in groups)
        {
            group.Children = group.Children.OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToList();
            group.Parameters = group.Parameters.OrderBy(p => p.SortOrder).ThenBy(p => p.Name).ToList();
            SortTree(group.Children);
        }
    }
}
