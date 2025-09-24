using Hems_Backend.Application.Sites.Commands.CreateSite;
using Hems_Backend.Application.Sites.Commands.UpdateSite;
using Hems_Backend.Application.Common.Exceptions;
using Hems_Backend.Domain.Entities;
using FluentAssertions;
using NUnit.Framework;

namespace Hems_Backend.Application.FunctionalTests.Sites;

using static Testing;

public class UpdateSiteTests : BaseTestFixture
{
    [Test]
    public async Task ShouldUpdateSite()
    {
        var userId = await RunAsDefaultUserAsync();

        var createCommand = new CreateSiteCommand
        {
            Name = "Original Site",
            Country = "Turkey",
            City = "Istanbul"
        };

        var siteId = await SendAsync(createCommand);

        var updateCommand = new UpdateSiteCommand
        {
            Id = siteId,
            Name = "Updated Site",
            Country = "Turkey",
            City = "Ankara",
            AddressLine = "456 Updated Street",
            Latitude = 39.9334m,
            Longitude = 32.8597m,
            Timezone = "Europe/Istanbul"
        };

        await SendAsync(updateCommand);

        var site = await FindAsync<Site>(siteId);

        site.Should().NotBeNull();
        site!.Name.Should().Be(updateCommand.Name);
        site.Country.Should().Be(updateCommand.Country);
        site.City.Should().Be(updateCommand.City);
        site.AddressLine.Should().Be(updateCommand.AddressLine);
        site.Latitude.Should().Be(updateCommand.Latitude);
        site.Longitude.Should().Be(updateCommand.Longitude);
        site.Timezone.Should().Be(updateCommand.Timezone);
    }

    [Test]
    public async Task ShouldThrowNotFoundExceptionForNonExistentSite()
    {
        var updateCommand = new UpdateSiteCommand
        {
            Id = Guid.NewGuid(),
            Name = "Non-existent Site"
        };

        await FluentActions.Invoking(() => SendAsync(updateCommand))
            .Should().ThrowAsync<Common.Exceptions.NotFoundException>();
    }

    [Test]
    public async Task ShouldRequireValidLatitude()
    {
        var userId = await RunAsDefaultUserAsync();

        var createCommand = new CreateSiteCommand
        {
            Name = "Test Site"
        };

        var siteId = await SendAsync(createCommand);

        var updateCommand = new UpdateSiteCommand
        {
            Id = siteId,
            Name = "Updated Site",
            Latitude = 91.0m // Invalid latitude
        };

        await FluentActions.Invoking(() => SendAsync(updateCommand))
            .Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task ShouldRequireValidLongitude()
    {
        var userId = await RunAsDefaultUserAsync();

        var createCommand = new CreateSiteCommand
        {
            Name = "Test Site"
        };

        var siteId = await SendAsync(createCommand);

        var updateCommand = new UpdateSiteCommand
        {
            Id = siteId,
            Name = "Updated Site",
            Longitude = 181.0m // Invalid longitude
        };

        await FluentActions.Invoking(() => SendAsync(updateCommand))
            .Should().ThrowAsync<ValidationException>();
    }
}









