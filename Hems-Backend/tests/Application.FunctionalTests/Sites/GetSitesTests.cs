using Hems_Backend.Application.Sites.Commands.CreateSite;
using Hems_Backend.Application.Sites.Queries.GetSites;
using Hems_Backend.Application.Common.Models;
using FluentAssertions;
using NUnit.Framework;

namespace Hems_Backend.Application.FunctionalTests.Sites;

using static Testing;

public class GetSitesTests : BaseTestFixture
{
    [Test]
    public async Task ShouldReturnSitesForUser()
    {
        var userId = await RunAsDefaultUserAsync();

        var site1 = new CreateSiteCommand
        {
            Name = "Site 1",
            Country = "Turkey",
            City = "Istanbul"
        };

        var site2 = new CreateSiteCommand
        {
            Name = "Site 2",
            Country = "Turkey",
            City = "Ankara"
        };

        await SendAsync(site1);
        await SendAsync(site2);

        var query = new GetSitesQuery
        {
            UserId = userId
        };

        var result = await SendAsync(query);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(x => x.UserId == userId);
    }

    [Test]
    public async Task ShouldReturnEmptyListForUserWithNoSites()
    {
        var userId = await RunAsDefaultUserAsync();

        var query = new GetSitesQuery
        {
            UserId = userId
        };

        var result = await SendAsync(query);

        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
    }

    [Test]
    public async Task ShouldNotReturnDeletedSites()
    {
        var userId = await RunAsDefaultUserAsync();

        var site = new CreateSiteCommand
        {
            Name = "Test Site"
        };

        var siteId = await SendAsync(site);

        // Soft delete the site
        await SendAsync(new Hems_Backend.Application.Sites.Commands.DeleteSite.DeleteSiteCommand(siteId));

        var query = new GetSitesQuery
        {
            UserId = userId
        };

        var result = await SendAsync(query);

        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
    }

    [Test]
    public async Task ShouldReturnPaginatedResults()
    {
        var userId = await RunAsDefaultUserAsync();

        // Create 5 sites
        for (int i = 1; i <= 5; i++)
        {
            var site = new CreateSiteCommand
            {
                Name = $"Site {i}"
            };
            await SendAsync(site);
        }

        var query = new GetSitesQuery
        {
            UserId = userId,
            PageNumber = 1,
            PageSize = 3
        };

        var result = await SendAsync(query);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(5);
        result.HasNextPage.Should().BeTrue();
    }
}









