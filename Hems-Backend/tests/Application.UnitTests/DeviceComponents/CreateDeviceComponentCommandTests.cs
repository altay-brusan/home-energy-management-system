using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.DeviceComponents.Commands.CreateDeviceComponent;
using Hems_Backend.Domain.Entities;
using Hems_Backend.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using System.Text.Json;

namespace Hems_Backend.Application.UnitTests.DeviceComponents;

public class CreateDeviceComponentCommandTests
{
    private Mock<IApplicationDbContext> _context = null!;
    private Mock<DbSet<DeviceComponent>> _dbSet = null!;
    private Mock<IUser> _user = null!;
    private TimeProvider _timeProvider = null!;

    [SetUp]
    public void Setup()
    {
        _context = new Mock<IApplicationDbContext>();
        _dbSet = new Mock<DbSet<DeviceComponent>>();
        _user = new Mock<IUser>();
        _timeProvider = TimeProvider.System;
        
        _context.Setup(x => x.DeviceComponents).Returns(_dbSet.Object);
        _context.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _user.Setup(x => x.Id).Returns("test-user-id");
    }

    [Test]
    public async Task Handle_ShouldCreateDeviceComponent_WhenValidCommand()
    {
        // Arrange
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

        var handler = new CreateDeviceComponentCommandHandler(_context.Object, _user.Object, _timeProvider);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.EqualTo(Guid.Empty));
        _dbSet.Verify(x => x.Add(It.IsAny<DeviceComponent>()), Times.Once);
        _context.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_ShouldCreateDeviceComponentWithMinimalData_WhenOnlyRequiredFieldsProvided()
    {
        // Arrange
        var command = new CreateDeviceComponentCommand
        {
            DeviceId = Guid.NewGuid(),
            DeviceType = DeviceType.Invertor,
            Name = "Test Invertor"
        };

        var handler = new CreateDeviceComponentCommandHandler(_context.Object, _user.Object, _timeProvider);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.EqualTo(Guid.Empty));
        _dbSet.Verify(x => x.Add(It.IsAny<DeviceComponent>()), Times.Once);
        _context.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
