using Hems_Backend.Application.Auth.Queries.GetUserStatus;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Domain.Entities;
using Hems_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using System.Linq.Expressions;

namespace Hems_Backend.Application.UnitTests.Auth;

public class GetUserStatusQueryTests
{
    private ApplicationDbContext _context = null!;
    private Mock<IUser> _user = null!;

    [SetUp]
    public void Setup()
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
        
        _user = new Mock<IUser>();
        _user.Setup(x => x.Id).Returns("test-user-id");
    }

    [TearDown]
    public async Task TearDown()
    {
        // Sadece bu test class'ının eklediği verileri temizle
        // Test verilerini unique identifier ile işaretleyelim
        var testUserId = "test-user-id";
        
        // Sadece bu test class'ının eklediği Sites'ları bul
        var testSites = _context.Sites.Where(s => s.UserId == testUserId).ToList();
        var testSiteIds = testSites.Select(s => s.Id).ToList();
        
        if (testSiteIds.Any())
        {
            var testSetups = _context.Setups.Where(s => testSiteIds.Contains(s.SiteId)).ToList();
            var testSetupIds = testSetups.Select(s => s.Id).ToList();
            
            if (testSetupIds.Any())
            {
                var testSetupDevices = _context.SetupDevices.Where(sd => testSetupIds.Contains(sd.SetupId)).ToList();
                var testDeviceIds = testSetupDevices.Select(sd => sd.DeviceId).ToList();
                
                // Sırayla sil (foreign key constraint'ler için)
                _context.SetupDevices.RemoveRange(testSetupDevices);
                if (testDeviceIds.Any())
                {
                    _context.Devices.RemoveRange(_context.Devices.Where(d => testDeviceIds.Contains(d.Id)));
                }
                _context.Setups.RemoveRange(testSetups);
            }
            _context.Sites.RemoveRange(testSites);
        }
        
        await _context.SaveChangesAsync();
        _context.Dispose();
    }

    [Test]
    public async Task Handle_ShouldReturnNeedsSite_WhenUserHasNoSites()
    {
        // Arrange
        var handler = new GetUserStatusQueryHandler(_context, _user.Object);

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
        var site = new Site
        {
            Id = siteId,
            UserId = "test-user-id",
            Name = "Test Site",
            Country = "Turkey",
            City = "Istanbul",
            AddressLine = "Test Address",
            Latitude = 41.0082m,
            Longitude = 28.9784m,
            Timezone = "Europe/Istanbul",
            IsDeleted = false
        };

        _context.Sites.Add(site);
        await _context.SaveChangesAsync();

        var handler = new GetUserStatusQueryHandler(_context, _user.Object);

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
        
        var site = new Site
        {
            Id = siteId,
            UserId = "test-user-id",
            Name = "Test Site",
            Country = "Turkey",
            City = "Istanbul",
            AddressLine = "Test Address",
            Latitude = 41.0082m,
            Longitude = 28.9784m,
            Timezone = "Europe/Istanbul",
            IsDeleted = false
        };

        var setup = new Setup
        {
            Id = setupId,
            SiteId = siteId,
            Name = "Test Setup"
        };

        _context.Sites.Add(site);
        _context.Setups.Add(setup);
        await _context.SaveChangesAsync();

        var handler = new GetUserStatusQueryHandler(_context, _user.Object);

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
        
        var site = new Site
        {
            Id = siteId,
            UserId = "test-user-id",
            Name = "Test Site",
            Country = "Turkey",
            City = "Istanbul",
            AddressLine = "Test Address",
            Latitude = 41.0082m,
            Longitude = 28.9784m,
            Timezone = "Europe/Istanbul",
            IsDeleted = false
        };

        var setup = new Setup
        {
            Id = setupId,
            SiteId = siteId,
            Name = "Test Setup"
        };

        // Device entity'sini oluştur
        var device = new Device
        {
            Id = deviceId,
            Manufacturer = "Test Manufacturer",
            ModelNumber = "Test Model",
            SerialNumber = "TEST123",
            QRCode = "QR123",
            TypeNumber = "TYPE123",
            Specifications = System.Text.Json.JsonDocument.Parse("{\"test\":\"value\"}"),
            InstallationDate = DateTime.UtcNow.AddDays(-30),
            CalibrationDate = DateTime.UtcNow.AddDays(-15),
            NextCheckoutDate = DateTime.UtcNow.AddDays(15),
            WarrantyPeriod = 24,
            WarrantyUnit = Domain.Enums.WarrantyUnit.Month,
            WarrantyStartDate = DateTime.UtcNow.AddDays(-30)
        };

        var setupDevice = new SetupDevice
        {
            Id = Guid.NewGuid(),
            SetupId = setupId,
            DeviceId = deviceId
        };

        _context.Sites.Add(site);
        _context.Setups.Add(setup);
        _context.Devices.Add(device);
        _context.SetupDevices.Add(setupDevice);
        await _context.SaveChangesAsync();

        var handler = new GetUserStatusQueryHandler(_context, _user.Object);

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
        Assert.That(result.Data.DeviceName, Is.EqualTo("Test Manufacturer Test Model"));
    }
}