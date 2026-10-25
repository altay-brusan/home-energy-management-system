using AutoMapper;
using FluentAssertions;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.DeviceSettings.Queries.GetDeviceParameters;
using Moq;
using NUnit.Framework;

namespace Hems_Backend.Application.UnitTests.DeviceSettings;

[TestFixture]
public class GetDeviceParametersQueryTests
{
    private Mock<IApplicationDbContext> _context = null!;
    private Mock<IMapper> _mapper = null!;
    private Mock<IDemoDataService> _demoDataService = null!;
    private Mock<IUser> _user = null!;
    private Mock<IIdentityService> _identityService = null!;

    [SetUp]
    public void Setup()
    {
        _context = new Mock<IApplicationDbContext>();
        _mapper = new Mock<IMapper>();
        _demoDataService = new Mock<IDemoDataService>();
        _user = new Mock<IUser>();
        _identityService = new Mock<IIdentityService>();
    }

    [Test]
    public async Task Handle_AdminUser_DemoDevice_ShouldReturnAllParameters()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        _user.Setup(x => x.Id).Returns("admin-user-id");
        _identityService.Setup(x => x.IsInRoleAsync("admin-user-id", "Administrator")).ReturnsAsync(true);
        _demoDataService.Setup(x => x.IsDemoDeviceAsync(deviceId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = new GetDeviceParametersQueryHandler(
            _context.Object, _mapper.Object, _demoDataService.Object,
            _user.Object, _identityService.Object);

        var query = new GetDeviceParametersQuery { DeviceId = deviceId };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert - Admin should see all parameters including grid config ones (GridVoltageHigh, GridVoltageLow)
        result.DeviceId.Should().Be(deviceId);
        result.Parameters.Should().Contain(p => p.ParameterName == "GridVoltageHigh");
        result.Parameters.Should().Contain(p => p.ParameterName == "GridVoltageLow");
        result.Parameters.Should().Contain(p => p.ParameterName == "BatteryCapacity");
    }

    [Test]
    public async Task Handle_NonAdminUser_DemoDevice_ShouldFilterGridConfigParameters()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        _user.Setup(x => x.Id).Returns("regular-user-id");
        _identityService.Setup(x => x.IsInRoleAsync("regular-user-id", "Administrator")).ReturnsAsync(false);
        _demoDataService.Setup(x => x.IsDemoDeviceAsync(deviceId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = new GetDeviceParametersQueryHandler(
            _context.Object, _mapper.Object, _demoDataService.Object,
            _user.Object, _identityService.Object);

        var query = new GetDeviceParametersQuery { DeviceId = deviceId };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert - Non-admin should NOT see grid config parameters
        result.DeviceId.Should().Be(deviceId);
        result.Parameters.Should().NotContain(p => RpcMethodMapping.IsGridConfigParameter(p.ParameterName));
        // But should still see non-grid parameters
        result.Parameters.Should().Contain(p => p.ParameterName == "BatteryCapacity");
        result.Parameters.Should().Contain(p => p.ParameterName == "BatteryMinSoc");
        result.Parameters.Should().Contain(p => p.ParameterName == "PvMaxPower");
    }

    [Test]
    public async Task Handle_NullUserId_DemoDevice_ShouldFilterGridConfigParameters()
    {
        // Arrange - simulates device-token or unauthenticated scenario
        var deviceId = Guid.NewGuid();
        _user.Setup(x => x.Id).Returns((string?)null);
        _demoDataService.Setup(x => x.IsDemoDeviceAsync(deviceId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = new GetDeviceParametersQueryHandler(
            _context.Object, _mapper.Object, _demoDataService.Object,
            _user.Object, _identityService.Object);

        var query = new GetDeviceParametersQuery { DeviceId = deviceId };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert - Null user ID should be treated as non-admin
        result.Parameters.Should().NotContain(p => RpcMethodMapping.IsGridConfigParameter(p.ParameterName));
        result.Parameters.Should().Contain(p => p.ParameterName == "BatteryCapacity");
        // IsInRoleAsync should never be called with null userId
        _identityService.Verify(x => x.IsInRoleAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task Handle_AdminUser_DemoDevice_ShouldReturnGridVoltageParameters()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        _user.Setup(x => x.Id).Returns("admin-user-id");
        _identityService.Setup(x => x.IsInRoleAsync("admin-user-id", "Administrator")).ReturnsAsync(true);
        _demoDataService.Setup(x => x.IsDemoDeviceAsync(deviceId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = new GetDeviceParametersQueryHandler(
            _context.Object, _mapper.Object, _demoDataService.Object,
            _user.Object, _identityService.Object);

        var query = new GetDeviceParametersQuery { DeviceId = deviceId };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert - Verify specific grid config params are present for admin
        var gridParams = result.Parameters.Where(p =>
            p.ParameterName == "GridVoltageHigh" || p.ParameterName == "GridVoltageLow").ToList();
        gridParams.Should().HaveCount(2);
    }

    [Test]
    public async Task Handle_NonAdminUser_DemoDevice_ShouldPreserveNonGridParameterCount()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        _user.Setup(x => x.Id).Returns("regular-user-id");
        _identityService.Setup(x => x.IsInRoleAsync("regular-user-id", "Administrator")).ReturnsAsync(false);
        _demoDataService.Setup(x => x.IsDemoDeviceAsync(deviceId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = new GetDeviceParametersQueryHandler(
            _context.Object, _mapper.Object, _demoDataService.Object,
            _user.Object, _identityService.Object);

        var query = new GetDeviceParametersQuery { DeviceId = deviceId };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert - Demo data has 8 total params, 2 are grid config (GridVoltageHigh, GridVoltageLow)
        // Non-admin should see 8 - 2 = 6 params
        // GridMaxExportPower is NOT a grid config param (not in RpcMethodMapping.GridConfigParameters)
        var nonGridParamNames = new[] { "GridMaxExportPower", "BatteryCapacity", "BatteryMinSoc", "BatteryMaxChargeCurrent", "PvMaxPower", "PvMpptCount" };
        foreach (var name in nonGridParamNames)
        {
            result.Parameters.Should().Contain(p => p.ParameterName == name, $"'{name}' should be visible to non-admin");
        }
    }
}
