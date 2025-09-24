using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.Devices.Commands.CreateDevice;
using Hems_Backend.Application.Devices.Queries.GetDevices;
using Hems_Backend.Domain.Entities;
using NUnit.Framework;

namespace Hems_Backend.Application.FunctionalTests.Devices;

using static Testing;

public class CreateDeviceTests : BaseTestFixture
{
    [Test]
    public async Task ShouldCreateDevice()
    {
        // Arrange
        var command = new CreateDeviceCommand
        {
            Manufacturer = "OTTOMOTIVE",
            ModelNumber = "MODEL-001",
            SerialNumber = "SN123456",
            TypeNumber = "TYPE-001",
            QRCode = "QR-CODE-001",
            InstallationDate = DateTime.UtcNow,
            CalibrationDate = DateTime.UtcNow
        };

        // Act
        var deviceId = await SendAsync(command);

        // Assert
        var device = await FindAsync<Device>(deviceId);
        device.Should().NotBeNull();
        device!.Manufacturer.Should().Be(command.Manufacturer);
        device.ModelNumber.Should().Be(command.ModelNumber);
        device.SerialNumber.Should().Be(command.SerialNumber);
        device.TypeNumber.Should().Be(command.TypeNumber);
        device.QRCode.Should().Be(command.QRCode);
        device.IsDeleted.Should().BeFalse();
    }

    [Test]
    public async Task ShouldGetDevices()
    {
        // Arrange
        await SendAsync(new CreateDeviceCommand
        {
            Manufacturer = "OTTOMOTIVE",
            ModelNumber = "MODEL-001",
            SerialNumber = "SN123456"
        });
        await SendAsync(new CreateDeviceCommand
        {
            Manufacturer = "TESLA",
            ModelNumber = "MODEL-002",
            SerialNumber = "SN789012"
        });

        var query = new GetDevicesQuery();

        // Act
        var result = await SendAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(x => !x.IsDeleted);
    }

    [Test]
    public async Task ShouldFilterDevicesByManufacturer()
    {
        // Arrange
        await SendAsync(new CreateDeviceCommand
        {
            Manufacturer = "OTTOMOTIVE",
            ModelNumber = "MODEL-001"
        });
        await SendAsync(new CreateDeviceCommand
        {
            Manufacturer = "TESLA",
            ModelNumber = "MODEL-002"
        });

        var query = new GetDevicesQuery { Manufacturer = "OTTOMOTIVE" };

        // Act
        var result = await SendAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items.Should().OnlyContain(x => x.Manufacturer == "OTTOMOTIVE");
    }
}
