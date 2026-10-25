using AutoMapper;
using Hems_Backend.Domain.Entities;

namespace Hems_Backend.Application.ParameterGroups.Queries.GetParameterGroups;

public class ParameterGroupMappingProfile : Profile
{
    public ParameterGroupMappingProfile()
    {
        CreateMap<ParameterGroup, ParameterGroupDto>();
        CreateMap<Parameter, ParameterDto>();
    }
}
