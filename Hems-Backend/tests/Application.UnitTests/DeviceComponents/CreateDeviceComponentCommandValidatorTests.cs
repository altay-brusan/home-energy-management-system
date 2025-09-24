using Hems_Backend.Application.DeviceComponents.Commands.CreateDeviceComponent;
using Hems_Backend.Domain.Enums;
using FluentAssertions;
using FluentValidation.TestHelper;
using NUnit.Framework;
using System.Text.Json;

namespace Hems_Backend.Application.UnitTests.DeviceComponents;

public class CreateDeviceComponentCommandValidatorTests
{
    private CreateDeviceComponentCommandValidator _validator;

    [SetUp]
    public void Setup()
    {
        _validator = new CreateDeviceComponentCommandValidator();
    }

    [Test]
    public void ShouldHaveErrorWhenDeviceIdIsEmpty()
    {
        var command = new CreateDeviceComponentCommand
        {
            DeviceId = Guid.Empty,
            DeviceType = DeviceType.Battery,
            Name = "Test Component"
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.DeviceId);
    }

    [Test]
    public void ShouldHaveErrorWhenNameIsEmpty()
    {
        var command = new CreateDeviceComponentCommand
        {
            DeviceId = Guid.NewGuid(),
            DeviceType = DeviceType.Battery,
            Name = string.Empty
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void ShouldHaveErrorWhenNameExceedsMaxLength()
    {
        var command = new CreateDeviceComponentCommand
        {
            DeviceId = Guid.NewGuid(),
            DeviceType = DeviceType.Battery,
            Name = new string('A', 201) // Exceeds 200 character limit
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void ShouldHaveErrorWhenManufacturerExceedsMaxLength()
    {
        var command = new CreateDeviceComponentCommand
        {
            DeviceId = Guid.NewGuid(),
            DeviceType = DeviceType.Battery,
            Name = "Test Component",
            Manufacturer = new string('A', 101) // Exceeds 100 character limit
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Manufacturer);
    }

    [Test]
    public void ShouldHaveErrorWhenModelNumberExceedsMaxLength()
    {
        var command = new CreateDeviceComponentCommand
        {
            DeviceId = Guid.NewGuid(),
            DeviceType = DeviceType.Battery,
            Name = "Test Component",
            ModelNumber = new string('A', 101) // Exceeds 100 character limit
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ModelNumber);
    }

    [Test]
    public void ShouldHaveErrorWhenSerialNumberExceedsMaxLength()
    {
        var command = new CreateDeviceComponentCommand
        {
            DeviceId = Guid.NewGuid(),
            DeviceType = DeviceType.Battery,
            Name = "Test Component",
            SerialNumber = new string('A', 101) // Exceeds 100 character limit
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.SerialNumber);
    }

    [Test]
    public void ShouldNotHaveErrorWhenCommandIsValid()
    {
        var command = new CreateDeviceComponentCommand
        {
            DeviceId = Guid.NewGuid(),
            DeviceType = DeviceType.Battery,
            Name = "Test Battery",
            Manufacturer = "Test Manufacturer",
            ModelNumber = "MODEL-001",
            SerialNumber = "SN123456",
            Specifications = JsonDocument.Parse("{\"capacity\": \"100Ah\"}"),
            WarrantyPeriod = 24,
            WarrantyUnit = WarrantyUnit.Month,
            WarrantyStartDate = DateTime.UtcNow
        };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void ShouldNotHaveErrorWhenCommandHasMinimalData()
    {
        var command = new CreateDeviceComponentCommand
        {
            DeviceId = Guid.NewGuid(),
            DeviceType = DeviceType.Invertor,
            Name = "Test Invertor"
        };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
