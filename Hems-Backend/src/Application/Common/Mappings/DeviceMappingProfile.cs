using AutoMapper;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Domain.Entities;

namespace Hems_Backend.Application.Common.Mappings;

public class DeviceMappingProfile : Profile
{
    public DeviceMappingProfile()
    {
        // Device component mapping
        CreateMap<DeviceComponent, DeviceComponentDto>()
            .ForMember(d => d.Created, opt => opt.MapFrom(s => s.Created.DateTime))
            .ForMember(d => d.LastModified, opt => opt.MapFrom(s => s.LastModified.DateTime));

        // ExternalDevice mapping
        CreateMap<ExternalDevice, ExternalDeviceDto>()
            .ForMember(d => d.Created, opt => opt.MapFrom(s => s.Created.DateTime))
            .ForMember(d => d.LastModified, opt => opt.MapFrom(s => s.LastModified.DateTime));

        // SetupDevice mapping (maps its Device and ExternalDevices via other maps)
        CreateMap<SetupDevice, SetupDeviceDto>()
            .ForMember(d => d.Created, opt => opt.MapFrom(s => s.Created.DateTime))
            .ForMember(d => d.LastModified, opt => opt.MapFrom(s => s.LastModified.DateTime));

        // Device -> DeviceDto
        // NOTE: intentionally do NOT map ThingsBoardDeviceId (kept internal).
        CreateMap<Device, DeviceDto>()
            .ForMember(d => d.DeviceComponents, opt => opt.MapFrom(s => s.DeviceComponents))
            .ForMember(d => d.SetupDevice, opt => opt.MapFrom(s => s.SetupDevice))
            .ForMember(d => d.Created, opt => opt.MapFrom(s => s.Created.DateTime))
            .ForMember(d => d.LastModified, opt => opt.MapFrom(s => (DateTime?)s.LastModified.DateTime));
    }
}
