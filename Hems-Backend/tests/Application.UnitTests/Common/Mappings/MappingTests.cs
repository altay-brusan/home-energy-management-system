using System.Reflection;
using System.Runtime.CompilerServices;
using AutoMapper;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.ParameterGroups.Queries.GetParameterGroups;
using Hems_Backend.Domain.Entities;
using NUnit.Framework;

namespace Hems_Backend.Application.UnitTests.Common.Mappings;

public class MappingTests
{
    private readonly IConfigurationProvider _configuration;
    private readonly IMapper _mapper;

    public MappingTests()
    {
        _configuration = new MapperConfiguration(config =>
            config.AddMaps(Assembly.GetAssembly(typeof(IApplicationDbContext))));

        _mapper = _configuration.CreateMapper();
    }

    [Test]
    public void ShouldHaveValidConfiguration()
    {
        _configuration.AssertConfigurationIsValid();
    }

    [Test]
    [TestCase(typeof(Setup), typeof(SetupDto))]
    [TestCase(typeof(ExternalDevice), typeof(ExternalDeviceDto))]
    [TestCase(typeof(DeviceComponent), typeof(DeviceComponentDto))]
    [TestCase(typeof(ParameterGroup), typeof(ParameterGroupDto))]
    [TestCase(typeof(Parameter), typeof(ParameterDto))]
    public void ShouldSupportMappingFromSourceToDestination(Type source, Type destination)
    {
        var instance = GetInstanceOf(source);

        _mapper.Map(instance, source, destination);
    }

    private object GetInstanceOf(Type type)
    {
        if (type.GetConstructor(Type.EmptyTypes) != null)
            return Activator.CreateInstance(type)!;

        // Type without parameterless constructor
        return RuntimeHelpers.GetUninitializedObject(type);
    }
}
