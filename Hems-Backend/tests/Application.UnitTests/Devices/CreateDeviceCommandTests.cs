using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Devices.Commands.CreateDevice;
using Hems_Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;

namespace Hems_Backend.Application.UnitTests.Devices;

public class CreateDeviceCommandTests
{
    private Mock<IApplicationDbContext> _context = null!;
    private Mock<DbSet<Device>> _dbSet = null!;
    private Mock<IUser> _user = null!;
    private TimeProvider _timeProvider = null!;

    [SetUp]
    public void Setup()
    {
        _context = new Mock<IApplicationDbContext>();
        _dbSet = new Mock<DbSet<Device>>();
        _user = new Mock<IUser>();
        _timeProvider = TimeProvider.System;
        
        _context.Setup(x => x.Devices).Returns(_dbSet.Object);
        _context.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _user.Setup(x => x.Id).Returns("test-user-id");
    }

    [Test]
    public async Task Handle_ShouldCreateDevice_WhenValidCommand()
    {
        // Arrange
        var command = new CreateDeviceCommand
        {
            Manufacturer = "OTTOMOTIVE",
            ModelNumber = "MODEL-001",
            SerialNumber = "SN123456",
            TypeNumber = "TYPE-001",
            QRCode = "QR-CODE-001",
            InstallationDate = DateTime.Now,
            CalibrationDate = DateTime.Now
        };

        var handler = new CreateDeviceCommandHandler(_context.Object, _user.Object, _timeProvider);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.EqualTo(Guid.Empty));
        _dbSet.Verify(x => x.Add(It.IsAny<Device>()), Times.Once);
        _context.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}



