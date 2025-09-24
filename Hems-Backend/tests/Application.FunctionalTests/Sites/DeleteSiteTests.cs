using Hems_Backend.Application.Sites.Commands.CreateSite;
using Hems_Backend.Application.Sites.Commands.DeleteSite;
using Hems_Backend.Application.Sites.Queries.GetSites;
using Hems_Backend.Application.Common.Exceptions;
using Hems_Backend.Domain.Entities;
using FluentAssertions;
using NUnit.Framework;

namespace Hems_Backend.Application.FunctionalTests.Sites;

using static Testing;

public class DeleteSiteTests : BaseTestFixture
{
    [Test]
    public async Task ShouldSoftDeleteSite()
    {
        var userId = await RunAsDefaultUserAsync();

        var createCommand = new CreateSiteCommand
        {
            Name = "Test Site",
            Country = "Turkey",
            City = "Istanbul"
        };

        var siteId = await SendAsync(createCommand);

        await SendAsync(new DeleteSiteCommand(siteId));

        var site = await FindAsync<Site>(siteId);

        site.Should().NotBeNull();
        site!.IsDeleted.Should().BeTrue();
    }

    [Test]
    public async Task ShouldThrowNotFoundExceptionForNonExistentSite()
    {
        var userId = await RunAsDefaultUserAsync();
        var deleteCommand = new DeleteSiteCommand(Guid.NewGuid());

        await FluentActions.Invoking(() => SendAsync(deleteCommand))
            .Should().ThrowAsync<Common.Exceptions.NotFoundException>();
    }

    [Test]
    public async Task ShouldNotReturnDeletedSiteInGetSites()
    {
        var userId = await RunAsDefaultUserAsync();

        var createCommand = new CreateSiteCommand
        {
            Name = "Test Site"
        };

        var siteId = await SendAsync(createCommand);

        // Verify site exists
        var query = new GetSitesQuery
        {
            UserId = userId
        };

        var resultBeforeDelete = await SendAsync(query);
        resultBeforeDelete.Items.Should().HaveCount(1);

        // Delete the site
        await SendAsync(new DeleteSiteCommand(siteId));

        // Verify site is not returned
        var resultAfterDelete = await SendAsync(query);
        resultAfterDelete.Items.Should().BeEmpty();
    }

    [Test]
    public async Task ShouldNotReturnDeletedSiteInGetSiteById()
    {
        var userId = await RunAsDefaultUserAsync();

        var createCommand = new CreateSiteCommand
        {
            Name = "Test Site"
        };

        var siteId = await SendAsync(createCommand);

        // Delete the site
        await SendAsync(new DeleteSiteCommand(siteId));

        // Try to get the deleted site
        var result = await SendAsync(new Hems_Backend.Application.Sites.Queries.GetSiteById.GetSiteByIdQuery(siteId));

        result.Should().BeNull();
    }
}









