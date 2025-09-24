using Hems_Backend.Application.Sites.Commands.CreateSite;
using Hems_Backend.Application.Sites.Queries.GetSites;
using Hems_Backend.Application.Common.Exceptions;
using Hems_Backend.Domain.Entities;
using FluentAssertions;
using NUnit.Framework;

namespace Hems_Backend.Application.FunctionalTests.Sites;

using static Testing;

public class CreateSiteTests : BaseTestFixture
{
    [Test]
    public async Task ShouldCreateSite()
    {
        var userId = await RunAsDefaultUserAsync();

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

        var siteId = await SendAsync(command);

        var site = await FindAsync<Site>(siteId);

        site.Should().NotBeNull();
        site!.UserId.Should().Be(userId);
        site.Name.Should().Be(command.Name);
        site.Country.Should().Be(command.Country);
        site.City.Should().Be(command.City);
        site.AddressLine.Should().Be(command.AddressLine);
        site.Latitude.Should().Be(command.Latitude);
        site.Longitude.Should().Be(command.Longitude);
        site.Timezone.Should().Be(command.Timezone);
        site.IsDeleted.Should().BeFalse();
    }

    [Test]
    public async Task ShouldRequireMinimumFields()
    {
        var command = new CreateSiteCommand();

        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task ShouldRequireValidLatitude()
    {
        await RunAsDefaultUserAsync();

        var command = new CreateSiteCommand
        {
            Name = "Test Site",
            Latitude = 91.0m // Invalid latitude
        };

        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task ShouldRequireValidLongitude()
    {
        await RunAsDefaultUserAsync();

        var command = new CreateSiteCommand
        {
            Name = "Test Site",
            Longitude = 181.0m // Invalid longitude
        };

        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task ShouldCreateSiteWithMinimalData()
    {
        var userId = await RunAsDefaultUserAsync();

        var command = new CreateSiteCommand
        {
            Name = "Minimal Site"
        };

        var siteId = await SendAsync(command);

        var site = await FindAsync<Site>(siteId);

        site.Should().NotBeNull();
        site!.UserId.Should().Be(userId);
        site.Name.Should().Be(command.Name);
        site.Timezone.Should().Be("Europe/Istanbul"); // Default value
        site.IsDeleted.Should().BeFalse();
    }
}









