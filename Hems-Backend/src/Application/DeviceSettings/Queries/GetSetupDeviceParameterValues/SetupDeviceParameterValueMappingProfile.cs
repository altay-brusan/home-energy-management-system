using AutoMapper;
using Hems_Backend.Domain.Entities;

namespace Hems_Backend.Application.DeviceSettings.Queries.GetSetupDeviceParameterValues;

public class SetupDeviceParameterValueMappingProfile : Profile
{
    public SetupDeviceParameterValueMappingProfile()
    {
        CreateMap<SetupDeviceParameterValue, SetupDeviceParameterValueDto>()
            .ForMember(dest => dest.ParameterName, opt => opt.MapFrom(src => src.Parameter.Name))
            .ForMember(dest => dest.ParameterGroupName, opt => opt.MapFrom(src => src.ParameterGroup != null ? src.ParameterGroup.Name : null));
    }
}
