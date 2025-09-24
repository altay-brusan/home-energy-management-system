using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.Setups.Commands.CreateSetup;
using Hems_Backend.Application.Setups.Queries.GetSetups;
using Hems_Backend.Application.Sites.Commands.CreateSite;
using Hems_Backend.Domain.Entities;
using NUnit.Framework;

namespace Hems_Backend.Application.FunctionalTests.Setups;

using static Testing;

public class CreateSetupTests : BaseTestFixture
{
    [Test]
    public async Task ShouldCreateSetup()
    {
        // Arrange
        var userId = await RunAsDefaultUserAsync();
        
        // First create a site
        var siteCommand = new CreateSiteCommand
        {
            Name = "Test Site"
        };
        var siteId = await SendAsync(siteCommand);

        var command = new CreateSetupCommand
        {
            SiteId = siteId,
            Name = "Test Setup",
            DeviceQRCode = "TEST-QR-001"
        };

        // Act
        var setupId = await SendAsync(command);

        // Assert
        var setup = await FindAsync<Setup>(setupId);
        setup.Should().NotBeNull();
        setup!.Name.Should().Be(command.Name);
        setup.SiteId.Should().Be(command.SiteId);
    }

    [Test]
    public async Task ShouldGetSetups()
    {
        // Arrange
        var userId = await RunAsDefaultUserAsync();
        
        // Create two different sites
        var site1Command = new CreateSiteCommand
        {
            Name = "Test Site 1"
        };
        var site1Id = await SendAsync(site1Command);

        var site2Command = new CreateSiteCommand
        {
            Name = "Test Site 2"
        };
        var site2Id = await SendAsync(site2Command);

        await SendAsync(new CreateSetupCommand
        {
            SiteId = site1Id,
            Name = "Setup 1",
            DeviceQRCode = "TEST-QR-002"
        });
        await SendAsync(new CreateSetupCommand
        {
            SiteId = site2Id,
            Name = "Setup 2",
            DeviceSerialNumber = "TEST-SERIAL-002"
        });

        var query = new GetSetupsQuery();

        // Act
        var result = await SendAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.Items.Should().Contain(x => x.SiteId == site1Id);
        result.Items.Should().Contain(x => x.SiteId == site2Id);
    }
}

