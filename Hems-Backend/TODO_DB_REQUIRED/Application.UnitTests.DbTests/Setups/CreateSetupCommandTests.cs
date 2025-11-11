using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Setups.Commands.CreateSetup;
using Hems_Backend.Domain.Entities;
using Hems_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace Hems_Backend.Application.UnitTests.Setups;

public class CreateSetupCommandTests
{
    private ApplicationDbContext _context = null!;
    private IUser _user = null!;
    private TimeProvider _timeProvider = null!;
    private Guid _testSiteId = Guid.Empty;

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

        // Test site'ı ekle
        var testSite = new Site
        {
            Id = Guid.NewGuid(),
            UserId = "test-user-id",
            Name = "Test Site",
            Country = "Turkey",
            City = "Istanbul",
            AddressLine = "Test Address",
            Latitude = 41.0082m,
            Longitude = 28.9784m,
            Timezone = "Europe/Istanbul",
            IsDeleted = false,
            CreatedBy = "test-user-id"
        };
        
        _context.Sites.Add(testSite);
        await _context.SaveChangesAsync();
        
        // Test site ID'sini sakla
        _testSiteId = testSite.Id;
        
        _user = new TestUser { Id = "test-user-id" };
        _timeProvider = TimeProvider.System;
    }

    [TearDown]
    public async Task TearDown()
    {
        // Sadece test verilerini temizle (test-user-id'ye ait olanları)
        var testSites = _context.Sites.Where(s => s.UserId == "test-user-id").ToList();
        var testSiteIds = testSites.Select(s => s.Id).ToList();
        
        var testSetups = _context.Setups.Where(s => testSiteIds.Contains(s.SiteId)).ToList();
        var testSetupIds = testSetups.Select(s => s.Id).ToList();
        
        var testSetupDevices = _context.SetupDevices.Where(sd => testSetupIds.Contains(sd.SetupId)).ToList();
        var testDeviceIds = testSetupDevices.Select(sd => sd.DeviceId).ToList();
        
        // Sırayla sil (foreign key constraint'ler için)
        _context.SetupDevices.RemoveRange(testSetupDevices);
        _context.Devices.RemoveRange(_context.Devices.Where(d => testDeviceIds.Contains(d.Id)));
        _context.Setups.RemoveRange(testSetups);
        _context.Sites.RemoveRange(testSites);
        
        await _context.SaveChangesAsync();
        _context.Dispose();
    }

    [Test]
    public async Task Handle_ShouldCreateSetup_WhenValidCommand()
    {
        // Arrange
        var command = new CreateSetupCommand
        {
            SiteId = _testSiteId, // Setup'ta eklenen site ID'sini kullan
            Name = "Test Setup"
        };

        var handler = new CreateSetupCommandHandler(_context, _user, _timeProvider);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.EqualTo(Guid.Empty));
        
        // Verify setup was created
        var createdSetup = await _context.Setups.FirstOrDefaultAsync(s => s.Id == result);
        Assert.That(createdSetup, Is.Not.Null);
        Assert.That(createdSetup.Name, Is.EqualTo("Test Setup"));
        Assert.That(createdSetup.SiteId, Is.EqualTo(_testSiteId));
    }
}

public class TestUser : IUser
{
    public string Id { get; set; } = string.Empty;
}

