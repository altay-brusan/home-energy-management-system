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
        // ParameterValueSettingSchemes'ları ParameterGroup ve Parameter ile birlikte getir
        var query = _context.ParameterValueSettingSchemes.AsQueryable();

        if (!string.IsNullOrEmpty(request.ModelNumber))
        {
            query = query.Where(pvss => pvss.ModelNumber == request.ModelNumber);
        }

        var parameterValueSettingSchemes = await query
            .Include(pvss => pvss.Parameter)
                .ThenInclude(p => p.ParameterGroup)
            .Include(pvss => pvss.ParameterGroup)
            .OrderBy(pvss => pvss.SortOrder)
            .ToListAsync(cancellationToken);

        // Hierarchical yapı oluştur
        var tree = BuildHierarchicalTree(parameterValueSettingSchemes);

        return new ParameterValueSettingSchemesTreeVm
        {
            ModelNumber = request.ModelNumber,
            ParameterGroups = tree
        };
    }

    private static List<ParameterGroupTreeDto> BuildHierarchicalTree(List<ParameterValueSettingScheme> parameterValueSettingSchemes)
    {
        // Tüm unique ParameterGroup'ları topla
        var allGroups = new Dictionary<Guid, ParameterGroupTreeDto>();

        foreach (var pvss in parameterValueSettingSchemes)
        {
            // Parameter'ın ParameterGroup'ını ekle
            if (pvss.Parameter.ParameterGroup != null)
            {
                var group = pvss.Parameter.ParameterGroup;
                if (!allGroups.ContainsKey(group.Id))
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
            }

            // PVSS'nin ParameterGroup'ını ekle (varsa)
            if (pvss.ParameterGroup != null)
            {
                var group = pvss.ParameterGroup;
                if (!allGroups.ContainsKey(group.Id))
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
            }
        }

        // Hierarchical yapı oluştur
        var rootGroups = new List<ParameterGroupTreeDto>();
        var groupLookup = allGroups.ToDictionary(g => g.Key, g => g.Value);

        foreach (var group in allGroups.Values)
        {
            if (group.ParentId == null)
            {
                rootGroups.Add(group);
            }
            else if (groupLookup.ContainsKey(group.ParentId.Value))
            {
                groupLookup[group.ParentId.Value].Children.Add(group);
            }
        }

        // Her grup için parameter'ları ekle
        foreach (var pvss in parameterValueSettingSchemes)
        {
            var parameterDto = new ParameterTreeDto
            {
                Id = pvss.Parameter.Id,
                Name = pvss.Parameter.Name,
                Description = pvss.Parameter.Description,
                ElementType = pvss.Parameter.ElementType,
                RoleType = pvss.Parameter.RoleType,
                Unit = pvss.Parameter.Unit,
                DefaultValue = pvss.Parameter.DefaultValue,
                SortOrder = pvss.Parameter.SortOrder,
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

            // Parameter'ın grubuna ekle
            if (pvss.Parameter.ParameterGroup != null)
            {
                var groupId = pvss.Parameter.ParameterGroup.Id;
                if (groupLookup.ContainsKey(groupId))
                {
                    groupLookup[groupId].Parameters.Add(parameterDto);
                }
            }
        }

        // Sort order'a göre sırala
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
