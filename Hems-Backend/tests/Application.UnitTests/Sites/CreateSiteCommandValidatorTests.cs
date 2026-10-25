using Hems_Backend.Application.Sites.Commands.CreateSite;
using FluentAssertions;
using FluentValidation.TestHelper;
using NUnit.Framework;

namespace Hems_Backend.Application.UnitTests.Sites;

public class CreateSiteCommandValidatorTests
{
    private CreateSiteCommandValidator _validator;

    [SetUp]
    public void Setup()
    {
        _validator = new CreateSiteCommandValidator();
    }

    [Test]
    public void ShouldHaveErrorWhenNameIsEmpty()
    {
        var command = new CreateSiteCommand
        {
            Name = string.Empty
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void ShouldHaveErrorWhenNameExceedsMaxLength()
    {
        var command = new CreateSiteCommand
        {
            Name = new string('A', 201) // Exceeds 200 character limit
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void ShouldHaveErrorWhenCountryExceedsMaxLength()
    {
        var command = new CreateSiteCommand
        {
            Name = "Test Site",
            Country = new string('A', 101) // Exceeds 100 character limit
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Country);
    }

    [Test]
    public void ShouldHaveErrorWhenCityExceedsMaxLength()
    {
        var command = new CreateSiteCommand
        {
            Name = "Test Site",
            City = new string('A', 101) // Exceeds 100 character limit
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.City);
    }

    [Test]
    public void ShouldHaveErrorWhenAddressLineExceedsMaxLength()
    {
        var command = new CreateSiteCommand
        {
            Name = "Test Site",
            AddressLine = new string('A', 501) // Exceeds 500 character limit
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.AddressLine);
    }

    [Test]
    public void ShouldHaveErrorWhenLatitudeIsOutOfRange()
    {
        var command = new CreateSiteCommand
        {
            Name = "Test Site",
            Latitude = 91.0m // Out of range
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Latitude);
    }

    [Test]
    public void ShouldHaveErrorWhenLongitudeIsOutOfRange()
    {
        var command = new CreateSiteCommand
        {
            Name = "Test Site",
            Longitude = 181.0m // Out of range
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Longitude);
    }

    [Test]
    public void ShouldHaveErrorWhenTimezoneIsEmpty()
    {
        var command = new CreateSiteCommand
        {
            Name = "Test Site",
            Timezone = string.Empty
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Timezone);
    }

    [Test]
    public void ShouldHaveErrorWhenTimezoneExceedsMaxLength()
    {
        var command = new CreateSiteCommand
        {
            Name = "Test Site",
            Timezone = new string('A', 51) // Exceeds 50 character limit
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Timezone);
    }

    [Test]
    public void ShouldNotHaveErrorWhenCommandIsValid()
    {
        var command = new CreateSiteCommand
        {
            Name = "Test Site",
            Country = "Turkey",
            City = "Istanbul",
            AddressLine = "123 Test Street",
            Latitude = 41.0082m,
            Longitude = 28.9784m,
            Timezone = "Europe/Istanbul"
        };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}









