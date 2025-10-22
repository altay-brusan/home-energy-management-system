using AutoMapper;
using Hems_Backend.Domain.Entities;

namespace Hems_Backend.Application.Parameters.Queries.GetParameters;

public class ParameterMappingProfile : Profile
{
    public ParameterMappingProfile()
    {
        CreateMap<Parameter, ParameterDto>()
            .ForMember(dest => dest.ParameterGroupName, opt => opt.MapFrom(src => src.ParameterGroup.Name));
    }
}
