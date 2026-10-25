using AutoMapper;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Domain.Entities;

namespace Hems_Backend.Application.Common.Mappings;

public class SetupMappingProfile : Profile
{
    public SetupMappingProfile()
    {
        CreateMap<Setup, SetupDto>()
            // map NodaTime Instant/Offset -> System.DateTime as done elsewhere
            .ForMember(d => d.Created, opt => opt.MapFrom(s => s.Created.DateTime))
            .ForMember(d => d.LastModified, opt => opt.MapFrom(s => (DateTime?)s.LastModified.DateTime))
            // map nested SetupDevice (DeviceMappingProfile already maps SetupDevice -> SetupDeviceDto)
            .ForMember(d => d.SetupDevice, opt => opt.MapFrom(s => s.SetupDevice));
    }
}
