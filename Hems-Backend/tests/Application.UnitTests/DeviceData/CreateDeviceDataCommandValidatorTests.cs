using System.Text.Json;
using FluentValidation.TestHelper;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.DeviceData.Commands.CreateDeviceData;
using Hems_Backend.Domain.Entities;
using Hems_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using System.Linq.Expressions;

namespace Hems_Backend.Application.UnitTests.DeviceData;

[TestFixture]
public class CreateDeviceDataCommandValidatorTests
{
    private CreateDeviceDataCommandValidator _validator;
    private ApplicationDbContext _context;

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
        
        // Test device'ı ekle
        var testDevice = new Device
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
        };
        
        _context.Devices.Add(testDevice);
        await _context.SaveChangesAsync();
        
        _validator = new CreateDeviceDataCommandValidator(_context);
    }

    [TearDown]
    public async Task TearDown()
    {
        // Sadece test device'larını temizle (belirli ID'li olanları)
        var testDeviceIds = new[] { 
            Guid.Parse("00000000-0000-0000-0000-000000000001") 
        };
        
        _context.Devices.RemoveRange(_context.Devices.Where(d => testDeviceIds.Contains(d.Id)));
        await _context.SaveChangesAsync();
        
        _context.Dispose();
    }

    [Test]
    public async Task Validate_ShouldPass_WhenValidCommand()
    {
        var command = new CreateDeviceDataCommand
        {
            DataType = "temperature_sensor",
            DeviceId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            RequestDate = DateTime.UtcNow.AddMinutes(-5),
            Data = JsonDocument.Parse("{\"temperature\": 25.5}")
        };

        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task Validate_ShouldFail_WhenDataTypeIsEmpty()
    {
        var command = new CreateDeviceDataCommand { DataType = string.Empty };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.DataType);
    }

    [Test]
    public async Task Validate_ShouldFail_WhenDataTypeExceedsMaxLength()
    {
        var command = new CreateDeviceDataCommand { DataType = new string('a', 101) };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.DataType);
    }

    [Test]
    public async Task Validate_ShouldFail_WhenDeviceIdIsEmpty()
    {
        var command = new CreateDeviceDataCommand { DeviceId = Guid.Empty };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.DeviceId);
    }

    [Test]
    public async Task Validate_ShouldFail_WhenRequestDateIsInFuture()
    {
        var command = new CreateDeviceDataCommand { RequestDate = DateTime.UtcNow.AddMinutes(10) };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.RequestDate);
    }

    [Test]
    public async Task Validate_ShouldPass_WhenRequestDateIsWithin5MinutesInFuture()
    {
        var command = new CreateDeviceDataCommand { RequestDate = DateTime.UtcNow.AddMinutes(3) };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveValidationErrorFor(x => x.RequestDate);
    }

    [Test]
    public async Task Validate_ShouldFail_WhenDataIsNull()
    {
        var command = new CreateDeviceDataCommand { Data = null! };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Data);
    }

    [Test]
    public async Task Validate_ShouldFail_WhenDataExceeds1MB()
    {
        var largeJson = new string('a', 1024 * 1024 + 1); // > 1MB
        var command = new CreateDeviceDataCommand { Data = JsonDocument.Parse($"{{\"largeData\": \"{largeJson}\"}}") };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Data);
    }
}