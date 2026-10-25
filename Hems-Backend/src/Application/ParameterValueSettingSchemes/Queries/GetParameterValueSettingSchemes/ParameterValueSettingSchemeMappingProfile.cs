using AutoMapper;
using Hems_Backend.Domain.Entities;

namespace Hems_Backend.Application.ParameterValueSettingSchemes.Queries.GetParameterValueSettingSchemes;

public class ParameterValueSettingSchemeMappingProfile : Profile
{
    public ParameterValueSettingSchemeMappingProfile()
    {
        CreateMap<ParameterValueSettingScheme, ParameterValueSettingSchemeDto>()
            .ForMember(dest => dest.ParameterName, opt => opt.MapFrom(src => src.Parameter.Name))
            .ForMember(dest => dest.ParameterGroupName, opt => opt.MapFrom(src => src.ParameterGroup != null ? src.ParameterGroup.Name : null));
    }
}
