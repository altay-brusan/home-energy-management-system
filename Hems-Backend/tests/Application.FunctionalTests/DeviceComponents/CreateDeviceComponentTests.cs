using Hems_Backend.Application.DeviceComponents.Commands.CreateDeviceComponent;
using Hems_Backend.Application.DeviceComponents.Queries.GetDeviceComponentsByDeviceId;
using Hems_Backend.Application.Common.Exceptions;
using Hems_Backend.Domain.Entities;
using Hems_Backend.Domain.Enums;
using FluentAssertions;
using NUnit.Framework;
using System.Text.Json;

namespace Hems_Backend.Application.FunctionalTests.DeviceComponents;

using static Testing;

public class CreateDeviceComponentTests : BaseTestFixture
{
    [Test]
    public async Task ShouldCreateDeviceComponent()
    {
        // Arrange
        var deviceId = await CreateTestDeviceAsync();

        var command = new CreateDeviceComponentCommand
        {
            DeviceId = deviceId,
            DeviceType = DeviceType.Battery,
            Name = "Test Battery",
            Manufacturer = "Test Manufacturer",
            ModelNumber = "MODEL-001",
            SerialNumber = "SN123456",
            Specifications = JsonDocument.Parse("{\"capacity\": \"100Ah\", \"voltage\": \"12V\"}"),
            WarrantyPeriod = 24,
            WarrantyUnit = WarrantyUnit.Month,
            WarrantyStartDate = DateTime.UtcNow,

        };

        // Act
        var componentId = await SendAsync(command);

        // Assert
        var component = await FindAsync<DeviceComponent>(componentId);
        component.Should().NotBeNull();
        component!.DeviceId.Should().Be(command.DeviceId);
        component.DeviceType.Should().Be(command.DeviceType);
        component.Name.Should().Be(command.Name);
        component.Manufacturer.Should().Be(command.Manufacturer);
        component.ModelNumber.Should().Be(command.ModelNumber);
        component.SerialNumber.Should().Be(command.SerialNumber);
        component.Specifications.Should().NotBeNull();
        component.WarrantyPeriod.Should().Be(command.WarrantyPeriod);
        component.WarrantyUnit.Should().Be(command.WarrantyUnit);
        component.WarrantyStartDate.Should().BeCloseTo(command.WarrantyStartDate!.Value, TimeSpan.FromMilliseconds(100));
        component.IsDeleted.Should().BeFalse();
    }

    [Test]
    public async Task ShouldRequireMinimumFields()
    {
        var command = new CreateDeviceComponentCommand();

        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task ShouldRequireValidDeviceId()
    {
        var command = new CreateDeviceComponentCommand
        {
            DeviceId = Guid.Empty,
            DeviceType = DeviceType.Battery,
            Name = "Test Component"
        };

        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task ShouldCreateDeviceComponentWithMinimalData()
    {
        // Arrange
        var deviceId = await CreateTestDeviceAsync();

        var command = new CreateDeviceComponentCommand
        {
            DeviceId = deviceId,
            DeviceType = DeviceType.Invertor,
            Name = "Test Invertor"
        };

        // Act
        var componentId = await SendAsync(command);

        // Assert
        var component = await FindAsync<DeviceComponent>(componentId);
        component.Should().NotBeNull();
        component!.DeviceId.Should().Be(command.DeviceId);
        component.DeviceType.Should().Be(command.DeviceType);
        component.Name.Should().Be(command.Name);
        component.Manufacturer.Should().BeNull();
        component.ModelNumber.Should().BeNull();
        component.SerialNumber.Should().BeNull();
        component.IsDeleted.Should().BeFalse();
    }

    [Test]
    public async Task ShouldGetDeviceComponents()
    {
        // Arrange
        var deviceId = await CreateTestDeviceAsync();

        await SendAsync(new CreateDeviceComponentCommand
        {
            DeviceId = deviceId,
            DeviceType = DeviceType.Battery,
            Name = "Battery Component"
        });
        await SendAsync(new CreateDeviceComponentCommand
        {
            DeviceId = deviceId,
            DeviceType = DeviceType.Invertor,
            Name = "Invertor Component"
        });

        var query = new GetDeviceComponentsByDeviceIdQuery { DeviceId = deviceId };

        // Act
        var result = await SendAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().OnlyContain(x => !x.IsDeleted);
        result.Should().OnlyContain(x => x.DeviceId == deviceId);
    }

    [Test]
    public async Task ShouldGetAllDeviceComponentsForDevice()
    {
        // Arrange
        var deviceId = await CreateTestDeviceAsync();

        await SendAsync(new CreateDeviceComponentCommand
        {
            DeviceId = deviceId,
            DeviceType = DeviceType.Battery,
            Name = "Battery Component"
        });
        await SendAsync(new CreateDeviceComponentCommand
        {
            DeviceId = deviceId,
            DeviceType = DeviceType.Invertor,
            Name = "Invertor Component"
        });

        var query = new GetDeviceComponentsByDeviceIdQuery 
        { 
            DeviceId = deviceId
        };

        // Act
        var result = await SendAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(x => x.DeviceType == DeviceType.Battery);
        result.Should().Contain(x => x.DeviceType == DeviceType.Invertor);
    }

    private async Task<Guid> CreateTestDeviceAsync()
    {
        var device = new Device
        {
            Id = Guid.NewGuid(),
            Manufacturer = "Test Manufacturer",
            ModelNumber = "TEST-MODEL",
            SerialNumber = "TEST-SERIAL",
            QRCode = "TEST-QR",
            Specifications = JsonDocument.Parse("{}"),
            IsDeleted = false,
            Created = DateTimeOffset.UtcNow,
            CreatedBy = "test-user"
        };

        await AddAsync(device);
        return device.Id;
    }
}
