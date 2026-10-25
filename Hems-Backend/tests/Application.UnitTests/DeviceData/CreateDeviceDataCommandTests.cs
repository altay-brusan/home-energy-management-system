using System.Text.Json;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.DeviceData.Commands.CreateDeviceData;
using Hems_Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;

namespace Hems_Backend.Application.UnitTests.DeviceData;

[TestFixture]
public class CreateDeviceDataCommandTests
{
    private Mock<IApplicationDbContext> _context;
    private Mock<DbSet<Hems_Backend.Domain.Entities.DeviceData>> _deviceDataDbSet;
    private Mock<ITimezoneService> _timezoneService;
    private Mock<IDeviceMappingService> _deviceMapping;
    private CreateDeviceDataCommandHandler _handler;

    [SetUp]
    public void SetUp()
    {
        _context = new Mock<IApplicationDbContext>();
        _deviceDataDbSet = new Mock<DbSet<Hems_Backend.Domain.Entities.DeviceData>>();
        _timezoneService = new Mock<ITimezoneService>();
        _deviceMapping = new Mock<IDeviceMappingService>();
        
        _context.Setup(c => c.DeviceData).Returns(_deviceDataDbSet.Object);
        
        // Mock timezone conversion to return same time (for tests)
        _timezoneService.Setup(x => x.ConvertUtcToDeviceTimeAsync(
            It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, DateTime dt, CancellationToken ct) => dt);
        
        // Mock device mapping to return a local device ID
        _deviceMapping.Setup(x => x.GetDeviceIdAsync(
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken ct) => id);
        
        _handler = new CreateDeviceDataCommandHandler(_context.Object, _timezoneService.Object, _deviceMapping.Object);
    }

    [Test]
    public async Task Handle_ShouldCreateDeviceData_WhenValidCommand()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var requestDate = DateTime.UtcNow;
        var data = JsonDocument.Parse("{\"temperature\": 25.5, \"humidity\": 60.2}");
        
        var command = new CreateDeviceDataCommand
        {
            DataType = "temperature_sensor",
            DeviceId = deviceId,
            DeviceLocalTime = requestDate,
            Data = data
        };

        var deviceDataList = new List<Hems_Backend.Domain.Entities.DeviceData>();
        
        _deviceDataDbSet.Setup(d => d.Add(It.IsAny<Hems_Backend.Domain.Entities.DeviceData>()))
            .Callback<Hems_Backend.Domain.Entities.DeviceData>(entity => 
            {
                entity.Id = 1; // Set ID for the entity
                deviceDataList.Add(entity);
            });
        
        _context.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.That(result, Is.GreaterThan(0));
        Assert.That(deviceDataList.Count, Is.EqualTo(1));
        
        var createdEntity = deviceDataList.First();
        Assert.That(createdEntity.DataType, Is.EqualTo("temperature_sensor"));
        Assert.That(createdEntity.ThingsBoardDeviceId, Is.EqualTo(deviceId));
        Assert.That(createdEntity.DeviceLocalTime, Is.EqualTo(requestDate));
        Assert.That(createdEntity.Data.RootElement.GetRawText(), Is.EqualTo(data.RootElement.GetRawText()));
        
        _deviceDataDbSet.Verify(d => d.Add(It.IsAny<Hems_Backend.Domain.Entities.DeviceData>()), Times.Once);
        _context.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_ShouldCreateDeviceData_WithBatteryInfo()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var requestDate = DateTime.UtcNow;
        var batteryData = JsonDocument.Parse(@"{
            ""Battery Pack Voltage"": 48.5,
            ""Battery Current"": 25.3,
            ""Soh"": 95.2,
            ""Soc"": [
                {
                    ""Cell Number"": 1,
                    ""Cell Voltage"": 3.65,
                    ""Cell Temperature"": 25.1
                }
            ],
            ""Total Charge Capacity"": ""1.38 KW"",
            ""Device Model"": ""BMS-48V-100Ah"",
            ""Device Serial Number"": ""BMS001234567"",
            ""Firmware Version"": ""v2.1.3""
        }");
        
        var command = new CreateDeviceDataCommand
        {
            DataType = "battery_info",
            DeviceId = deviceId,
            DeviceLocalTime = requestDate,
            Data = batteryData
        };

        var deviceDataList = new List<Hems_Backend.Domain.Entities.DeviceData>();
        
        _deviceDataDbSet.Setup(d => d.Add(It.IsAny<Hems_Backend.Domain.Entities.DeviceData>()))
            .Callback<Hems_Backend.Domain.Entities.DeviceData>(entity => 
            {
                entity.Id = 1; // Set ID for the entity
                deviceDataList.Add(entity);
            });
        
        _context.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.That(result, Is.GreaterThan(0));
        Assert.That(deviceDataList.Count, Is.EqualTo(1));
        
        var createdEntity = deviceDataList.First();
        Assert.That(createdEntity.DataType, Is.EqualTo("battery_info"));
        Assert.That(createdEntity.ThingsBoardDeviceId, Is.EqualTo(deviceId));
        Assert.That(createdEntity.DeviceLocalTime, Is.EqualTo(requestDate));
        
        // Verify JSON data structure
        var jsonData = createdEntity.Data.RootElement;
        Assert.That(jsonData.GetProperty("Battery Pack Voltage").GetDouble(), Is.EqualTo(48.5));
        Assert.That(jsonData.GetProperty("Battery Current").GetDouble(), Is.EqualTo(25.3));
        Assert.That(jsonData.GetProperty("Soh").GetDouble(), Is.EqualTo(95.2));
        Assert.That(jsonData.GetProperty("Device Model").GetString(), Is.EqualTo("BMS-48V-100Ah"));
    }

    [Test]
    public async Task Handle_ShouldCreateDeviceData_WithInverterInfo()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var requestDate = DateTime.UtcNow;
        var inverterData = JsonDocument.Parse(@"{
            ""Last Inverter Startup Time"": ""2025-01-25T08:30:15.000Z"",
            ""Active Power"": 2500.5,
            ""Reactive Power"": 150.2,
            ""Power Factor"": 0.95,
            ""Phases"": [
                {
                    ""Phase Id"": ""A"",
                    ""Voltage"": 230.1,
                    ""Current"": 10.8
                }
            ],
            ""Insulation Resistance"": 2.5,
            ""Device Serial Number"": ""INV001234567"",
            ""Firmware Version"": ""v3.2.1""
        }");
        
        var command = new CreateDeviceDataCommand
        {
            DataType = "inverter_realtime_info",
            DeviceId = deviceId,
            DeviceLocalTime = requestDate,
            Data = inverterData
        };

        var deviceDataList = new List<Hems_Backend.Domain.Entities.DeviceData>();
        
        _deviceDataDbSet.Setup(d => d.Add(It.IsAny<Hems_Backend.Domain.Entities.DeviceData>()))
            .Callback<Hems_Backend.Domain.Entities.DeviceData>(entity => 
            {
                entity.Id = 1; // Set ID for the entity
                deviceDataList.Add(entity);
            });
        
        _context.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.That(result, Is.GreaterThan(0));
        Assert.That(deviceDataList.Count, Is.EqualTo(1));
        
        var createdEntity = deviceDataList.First();
        Assert.That(createdEntity.DataType, Is.EqualTo("inverter_realtime_info"));
        Assert.That(createdEntity.ThingsBoardDeviceId, Is.EqualTo(deviceId));
        Assert.That(createdEntity.DeviceLocalTime, Is.EqualTo(requestDate));
        
        // Verify JSON data structure
        var jsonData = createdEntity.Data.RootElement;
        Assert.That(jsonData.GetProperty("Active Power").GetDouble(), Is.EqualTo(2500.5));
        Assert.That(jsonData.GetProperty("Reactive Power").GetDouble(), Is.EqualTo(150.2));
        Assert.That(jsonData.GetProperty("Power Factor").GetDouble(), Is.EqualTo(0.95));
        Assert.That(jsonData.GetProperty("Device Serial Number").GetString(), Is.EqualTo("INV001234567"));
    }

    [Test]
    public async Task Handle_ShouldReturnEntityId_AfterSaveChanges()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var command = new CreateDeviceDataCommand
        {
            DataType = "test_data",
            DeviceId = deviceId,
            DeviceLocalTime = DateTime.UtcNow,
            Data = JsonDocument.Parse("{\"test\": \"value\"}")
        };

        var deviceDataList = new List<Hems_Backend.Domain.Entities.DeviceData>();
        
        _deviceDataDbSet.Setup(d => d.Add(It.IsAny<Hems_Backend.Domain.Entities.DeviceData>()))
            .Callback<Hems_Backend.Domain.Entities.DeviceData>(entity => 
            {
                // Simulate entity getting an ID after save
                entity.GetType().GetProperty("Id")?.SetValue(entity, 123);
                deviceDataList.Add(entity);
            });
        
        _context.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.That(result, Is.EqualTo(123));
    }
}
