using Hems_Backend.Application.Auth.Queries.GetUserStatus;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using System.Text.Json;

namespace Hems_Backend.Application.UnitTests.Auth;

public class GetUserStatusQueryTests
{
    private Mock<IApplicationDbContext> _context = null!;
    private Mock<DbSet<Site>> _sitesDbSet = null!;
    private Mock<DbSet<Setup>> _setupsDbSet = null!;
    private Mock<DbSet<SetupDevice>> _setupDevicesDbSet = null!;
    private Mock<IUser> _user = null!;

    [SetUp]
    public void Setup()
    {
        _context = new Mock<IApplicationDbContext>();
        _sitesDbSet = new Mock<DbSet<Site>>();
        _setupsDbSet = new Mock<DbSet<Setup>>();
        _setupDevicesDbSet = new Mock<DbSet<SetupDevice>>();
        _user = new Mock<IUser>();
        
        _context.Setup(x => x.Sites).Returns(_sitesDbSet.Object);
        _context.Setup(x => x.Setups).Returns(_setupsDbSet.Object);
        _context.Setup(x => x.SetupDevices).Returns(_setupDevicesDbSet.Object);
        _user.Setup(x => x.Id).Returns("test-user-id");
    }

    [Test]
    public async Task Handle_ShouldReturnNeedsSite_WhenUserHasNoSites()
    {
        // Arrange
        var sites = new List<Site>().AsQueryable();
        _sitesDbSet.Setup(x => x.AsQueryable()).Returns(sites);

        var handler = new GetUserStatusQueryHandler(_context.Object, _user.Object);

        // Act
        var result = await handler.Handle(new GetUserStatusQuery(), CancellationToken.None);

        // Assert
        Assert.That(result.Status, Is.EqualTo(UserStatusType.NeedsSite));
        Assert.That(result.RedirectTo, Is.EqualTo("create_site"));
        Assert.That(result.Message, Is.EqualTo("Site oluşturmanız gerekiyor"));
        Assert.That(result.Data, Is.Null);
    }

    [Test]
    public async Task Handle_ShouldReturnNeedsSetup_WhenUserHasSiteButNoSetup()
    {
        // Arrange
        var siteId = Guid.NewGuid();
        var sites = new List<Site>
        {
            new Site
            {
                Id = siteId,
                UserId = "test-user-id",
                Name = "Test Site",
                IsDeleted = false
            }
        }.AsQueryable();

        var setups = new List<Setup>().AsQueryable();

        _sitesDbSet.Setup(x => x.AsQueryable()).Returns(sites);
        _setupsDbSet.Setup(x => x.AsQueryable()).Returns(setups);

        var handler = new GetUserStatusQueryHandler(_context.Object, _user.Object);

        // Act
        var result = await handler.Handle(new GetUserStatusQuery(), CancellationToken.None);

        // Assert
        Assert.That(result.Status, Is.EqualTo(UserStatusType.NeedsSetup));
        Assert.That(result.RedirectTo, Is.EqualTo("create_setup"));
        Assert.That(result.Message, Is.EqualTo("Device bağlamanız gerekiyor"));
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data.SiteId, Is.EqualTo(siteId));
        Assert.That(result.Data.SiteName, Is.EqualTo("Test Site"));
    }

    [Test]
    public async Task Handle_ShouldReturnNeedsSetup_WhenUserHasSiteAndSetupButNoDevice()
    {
        // Arrange
        var siteId = Guid.NewGuid();
        var setupId = Guid.NewGuid();
        
        var sites = new List<Site>
        {
            new Site
            {
                Id = siteId,
                UserId = "test-user-id",
                Name = "Test Site",
                IsDeleted = false
            }
        }.AsQueryable();

        var setups = new List<Setup>
        {
            new Setup
            {
                Id = setupId,
                SiteId = siteId,
                Name = "Test Setup"
            }
        }.AsQueryable();

        var setupDevices = new List<SetupDevice>().AsQueryable();

        _sitesDbSet.Setup(x => x.AsQueryable()).Returns(sites);
        _setupsDbSet.Setup(x => x.AsQueryable()).Returns(setups);
        _setupDevicesDbSet.Setup(x => x.AsQueryable()).Returns(setupDevices);

        var handler = new GetUserStatusQueryHandler(_context.Object, _user.Object);

        // Act
        var result = await handler.Handle(new GetUserStatusQuery(), CancellationToken.None);

        // Assert
        Assert.That(result.Status, Is.EqualTo(UserStatusType.NeedsSetup));
        Assert.That(result.RedirectTo, Is.EqualTo("create_setup"));
        Assert.That(result.Message, Is.EqualTo("Device bağlamanız gerekiyor"));
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data.SiteId, Is.EqualTo(siteId));
        Assert.That(result.Data.SiteName, Is.EqualTo("Test Site"));
        Assert.That(result.Data.SetupId, Is.EqualTo(setupId));
        Assert.That(result.Data.SetupName, Is.EqualTo("Test Setup"));
    }

    [Test]
    public async Task Handle_ShouldReturnComplete_WhenUserHasCompleteSetup()
    {
        // Arrange
        var siteId = Guid.NewGuid();
        var setupId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        
        var sites = new List<Site>
        {
            new Site
            {
                Id = siteId,
                UserId = "test-user-id",
                Name = "Test Site",
                IsDeleted = false
            }
        }.AsQueryable();

        var setups = new List<Setup>
        {
            new Setup
            {
                Id = setupId,
                SiteId = siteId,
                Name = "Test Setup"
            }
        }.AsQueryable();

        var setupDevices = new List<SetupDevice>
        {
            new SetupDevice
            {
                Id = Guid.NewGuid(),
                SetupId = setupId,
                DeviceId = deviceId,
                Device = new Device
                {
                    Id = deviceId,
                    Manufacturer = "Test Manufacturer",
                    ModelNumber = "TEST-MODEL"
                }
            }
        }.AsQueryable();

        _sitesDbSet.Setup(x => x.AsQueryable()).Returns(sites);
        _setupsDbSet.Setup(x => x.AsQueryable()).Returns(setups);
        _setupDevicesDbSet.Setup(x => x.AsQueryable()).Returns(setupDevices);

        var handler = new GetUserStatusQueryHandler(_context.Object, _user.Object);

        // Act
        var result = await handler.Handle(new GetUserStatusQuery(), CancellationToken.None);

        // Assert
        Assert.That(result.Status, Is.EqualTo(UserStatusType.Complete));
        Assert.That(result.RedirectTo, Is.EqualTo("dashboard"));
        Assert.That(result.Message, Is.EqualTo("Hoş geldiniz!"));
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data.SiteId, Is.EqualTo(siteId));
        Assert.That(result.Data.SiteName, Is.EqualTo("Test Site"));
        Assert.That(result.Data.SetupId, Is.EqualTo(setupId));
        Assert.That(result.Data.SetupName, Is.EqualTo("Test Setup"));
        Assert.That(result.Data.DeviceId, Is.EqualTo(deviceId));
        Assert.That(result.Data.DeviceName, Is.EqualTo("Test Manufacturer TEST-MODEL"));
    }
}
