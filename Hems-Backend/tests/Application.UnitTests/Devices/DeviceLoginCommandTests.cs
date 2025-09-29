using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Services;
using Hems_Backend.Application.Devices.Commands.RegisterDevice;
using Hems_Backend.Domain.Entities;
using Hems_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Text.Json;

namespace Hems_Backend.Application.UnitTests.Devices;

[TestFixture]
public class DeviceLoginCommandTests
{
    private ApplicationDbContext _context = null!;
    private Mock<IDeviceJwtService> _deviceJwtService = null!;
    private DeviceLoginCommandHandler _handler = null!;

    [SetUp]
    public async Task Setup()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Server=localhost;Port=5433;User ID=postgres;Password=***REMOVED***;Database=HEMS;Pooling=true;";

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
        
        // Test device'ları ekle
        var testDevices = new List<Device>
        {
            new Device
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Manufacturer = "Test Manufacturer",
                ModelNumber = "Test Model",
                SerialNumber = "TEST123",
                QRCode = "QR123",
                TypeNumber = "TYPE123",
                Specifications = JsonDocument.Parse("{\"test\":\"value\"}"),
                InstallationDate = DateTime.UtcNow.AddDays(-30),
                CalibrationDate = DateTime.UtcNow.AddDays(-15),
                NextCheckoutDate = DateTime.UtcNow.AddDays(15),
                WarrantyPeriod = 24,
                WarrantyUnit = Domain.Enums.WarrantyUnit.Month,
                WarrantyStartDate = DateTime.UtcNow.AddDays(-30),
                IsDeleted = false
            },
            new Device
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                Manufacturer = "Test Manufacturer 2",
                ModelNumber = "Test Model 2",
                SerialNumber = "TEST456",
                QRCode = "QR456",
                TypeNumber = "TYPE456",
                Specifications = JsonDocument.Parse("{\"test\":\"value2\"}"),
                InstallationDate = DateTime.UtcNow.AddDays(-30),
                CalibrationDate = DateTime.UtcNow.AddDays(-15),
                NextCheckoutDate = DateTime.UtcNow.AddDays(15),
                WarrantyPeriod = 24,
                WarrantyUnit = Domain.Enums.WarrantyUnit.Month,
                WarrantyStartDate = DateTime.UtcNow.AddDays(-30),
                IsDeleted = true // Deleted device
            }
        };
        
        _context.Devices.AddRange(testDevices);
        await _context.SaveChangesAsync();
        
        _deviceJwtService = new Mock<IDeviceJwtService>();
        _handler = new DeviceLoginCommandHandler(_context, _deviceJwtService.Object, Mock.Of<ILogger<DeviceLoginCommandHandler>>());
    }

    [TearDown]
    public async Task TearDown()
    {
        // Sadece test device'larını temizle (belirli ID'li olanları)
        var testDeviceIds = new[] { 
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            Guid.Parse("00000000-0000-0000-0000-000000000002")
        };
        
        _context.Devices.RemoveRange(_context.Devices.Where(d => testDeviceIds.Contains(d.Id)));
        await _context.SaveChangesAsync();
        
        _context.Dispose();
    }

    [Test]
    public async Task Handle_ShouldReturnSuccess_WhenValidSerialNumberAndQRCode()
    {
        // Arrange
        var command = new DeviceLoginCommand
        {
            SerialNumber = "TEST123",
            QRCode = "QR123"
        };

        var jwtResult = new DeviceJwtResult
        {
            AccessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
            RefreshToken = "refresh_token_123",
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        _deviceJwtService.Setup(x => x.GenerateTokenAsync(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(jwtResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.DeviceId, Is.EqualTo(Guid.Parse("00000000-0000-0000-0000-000000000001")));
        Assert.That(result.DeviceName, Is.EqualTo("Test Manufacturer Test Model"));
        Assert.That(result.AccessToken, Is.EqualTo(jwtResult.AccessToken));
        Assert.That(result.RefreshToken, Is.EqualTo(jwtResult.RefreshToken));
        Assert.That(result.ExpiresAt, Is.EqualTo(jwtResult.ExpiresAt));
    }

    [Test]
    public async Task Handle_ShouldReturnSuccess_WhenValidSerialNumberOnly()
    {
        // Arrange
        var command = new DeviceLoginCommand
        {
            SerialNumber = "TEST123",
            QRCode = null
        };

        var jwtResult = new DeviceJwtResult
        {
            AccessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
            RefreshToken = "refresh_token_123",
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        _deviceJwtService.Setup(x => x.GenerateTokenAsync(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(jwtResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.DeviceId, Is.EqualTo(Guid.Parse("00000000-0000-0000-0000-000000000001")));
        Assert.That(result.DeviceName, Is.EqualTo("Test Manufacturer Test Model"));
        Assert.That(result.AccessToken, Is.EqualTo(jwtResult.AccessToken));
        Assert.That(result.RefreshToken, Is.EqualTo(jwtResult.RefreshToken));
        Assert.That(result.ExpiresAt, Is.EqualTo(jwtResult.ExpiresAt));
    }

    [Test]
    public async Task Handle_ShouldReturnFailure_WhenDeviceNotFound()
    {
        // Arrange
        var command = new DeviceLoginCommand
        {
            SerialNumber = "NONEXISTENT",
            QRCode = "NONEXISTENT"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.DeviceId, Is.Null);
        Assert.That(result.DeviceName, Is.Null);
        Assert.That(result.AccessToken, Is.Null);
        Assert.That(result.RefreshToken, Is.Null);
        Assert.That(result.ExpiresAt, Is.Null);
        Assert.That(result.ErrorMessage, Is.EqualTo("Device not found"));
    }

    [Test]
    public async Task Handle_ShouldReturnFailure_WhenDeviceIsDeleted()
    {
        // Arrange
        var command = new DeviceLoginCommand
        {
            SerialNumber = "TEST456", // Bu deleted device'ın SerialNumber'ı
            QRCode = "QR456" // Bu deleted device'ın QRCode'u
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.DeviceId, Is.Null);
        Assert.That(result.DeviceName, Is.Null);
        Assert.That(result.AccessToken, Is.Null);
        Assert.That(result.RefreshToken, Is.Null);
        Assert.That(result.ExpiresAt, Is.Null);
        Assert.That(result.ErrorMessage, Is.EqualTo("Device is no longer active"));
    }
}