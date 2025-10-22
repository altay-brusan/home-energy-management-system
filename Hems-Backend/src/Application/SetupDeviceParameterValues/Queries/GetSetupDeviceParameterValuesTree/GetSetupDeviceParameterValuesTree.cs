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
        // SetupDeviceParameterValues'ları ParameterGroup ve Parameter ile birlikte getir
        var setupDeviceParameterValues = await _context.SetupDeviceParameterValues
            .Where(sdpv => sdpv.SetupDeviceId == request.SetupDeviceId)
            .Include(sdpv => sdpv.Parameter)
                .ThenInclude(p => p.ParameterGroup)
            .Include(sdpv => sdpv.ParameterGroup)
            .OrderBy(sdpv => sdpv.SortOrder)
            .ToListAsync(cancellationToken);

        // Hierarchical yapı oluştur
        var tree = BuildHierarchicalTree(setupDeviceParameterValues);

        return new SetupDeviceParameterValuesTreeVm
        {
            SetupDeviceId = request.SetupDeviceId,
            ParameterGroups = tree
        };
    }

    private static List<ParameterGroupTreeDto> BuildHierarchicalTree(List<SetupDeviceParameterValue> setupDeviceParameterValues)
    {
        // Tüm unique ParameterGroup'ları topla
        var allGroups = new Dictionary<Guid, ParameterGroupTreeDto>();

        foreach (var sdpv in setupDeviceParameterValues)
        {
            // Parameter'ın ParameterGroup'ını ekle
            if (sdpv.Parameter.ParameterGroup != null)
            {
                var group = sdpv.Parameter.ParameterGroup;
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

            // SDPV'nin ParameterGroup'ını ekle (varsa)
            if (sdpv.ParameterGroup != null)
            {
                var group = sdpv.ParameterGroup;
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
        foreach (var sdpv in setupDeviceParameterValues)
        {
            var parameterDto = new ParameterTreeDto
            {
                Id = sdpv.Parameter.Id,
                Name = sdpv.Parameter.Name,
                Description = sdpv.Parameter.Description,
                ElementType = sdpv.Parameter.ElementType,
                RoleType = sdpv.Parameter.RoleType,
                Unit = sdpv.Parameter.Unit,
                DefaultValue = sdpv.Parameter.DefaultValue,
                SortOrder = sdpv.Parameter.SortOrder,
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

            // Parameter'ın grubuna ekle
            if (sdpv.Parameter.ParameterGroup != null)
            {
                var groupId = sdpv.Parameter.ParameterGroup.Id;
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
