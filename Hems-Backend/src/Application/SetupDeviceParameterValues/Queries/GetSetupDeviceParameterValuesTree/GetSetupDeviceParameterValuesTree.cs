using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Mappings;
using Hems_Backend.Domain.Entities;
using MediatR;

namespace Hems_Backend.Application.SetupDeviceParameterValues.Queries.GetSetupDeviceParameterValuesTree;

public record GetSetupDeviceParameterValuesTreeQuery : IRequest<SetupDeviceParameterValuesTreeVm>
{
    public Guid SetupDeviceId { get; init; }
}

public class GetSetupDeviceParameterValuesTreeQueryHandler : IRequestHandler<GetSetupDeviceParameterValuesTreeQuery, SetupDeviceParameterValuesTreeVm>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetSetupDeviceParameterValuesTreeQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<SetupDeviceParameterValuesTreeVm> Handle(GetSetupDeviceParameterValuesTreeQuery request, CancellationToken cancellationToken)
    {
        // 1. SetupDeviceParameterValues'ları çek
        var setupDeviceParameterValues = await _context.SetupDeviceParameterValues
            .Where(sdpv => sdpv.SetupDeviceId == request.SetupDeviceId)
            .Include(sdpv => sdpv.Parameter)
            .ToListAsync(cancellationToken);

        if (!setupDeviceParameterValues.Any())
        {
            return new SetupDeviceParameterValuesTreeVm
            {
                SetupDeviceId = request.SetupDeviceId,
                ParameterGroups = new List<ParameterGroupTreeDto>()
            };
        }

        // 2. Bu parameter'ların ID'lerini al
        var parameterIds = setupDeviceParameterValues.Select(sdpv => sdpv.ParameterId).ToList();

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
        var tree = BuildHierarchicalTree(setupDeviceParameterValues, parameters, allParameterGroups);

        return new SetupDeviceParameterValuesTreeVm
        {
            SetupDeviceId = request.SetupDeviceId,
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
        List<SetupDeviceParameterValue> setupDeviceParameterValues,
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
        
        foreach (var sdpv in setupDeviceParameterValues)
        {
            if (parameterLookup.ContainsKey(sdpv.ParameterId))
            {
                var parameter = parameterLookup[sdpv.ParameterId];
                
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
                        SetupDeviceParameterValue = new SetupDeviceParameterValueTreeDto
                        {
                            Id = sdpv.Id,
                            Value = sdpv.Value,
                            SortOrder = sdpv.SortOrder,
                            ModelNumber = sdpv.ModelNumber,
                            Created = sdpv.Created,
                            CreatedBy = sdpv.CreatedBy,
                            LastModified = sdpv.LastModified,
                            LastModifiedBy = sdpv.LastModifiedBy
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
