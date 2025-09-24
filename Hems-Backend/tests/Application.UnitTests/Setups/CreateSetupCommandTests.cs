using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Setups.Commands.CreateSetup;
using Hems_Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;

namespace Hems_Backend.Application.UnitTests.Setups;

public class CreateSetupCommandTests
{
    private Mock<IApplicationDbContext> _context = null!;
    private Mock<DbSet<Setup>> _dbSet = null!;
    private Mock<IUser> _user = null!;
    private TimeProvider _timeProvider = null!;

    [SetUp]
    public void Setup()
    {
        _context = new Mock<IApplicationDbContext>();
        _dbSet = new Mock<DbSet<Setup>>();
        _user = new Mock<IUser>();
        _timeProvider = TimeProvider.System;
        
        _context.Setup(x => x.Setups).Returns(_dbSet.Object);
        _context.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _user.Setup(x => x.Id).Returns("test-user-id");
    }

    [Test]
    public async Task Handle_ShouldCreateSetup_WhenValidCommand()
    {
        // Arrange
        var command = new CreateSetupCommand
        {
            SiteId = Guid.NewGuid(),
            Name = "Test Setup"
        };

        var handler = new CreateSetupCommandHandler(_context.Object, _user.Object, _timeProvider);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.EqualTo(Guid.Empty));
        _dbSet.Verify(x => x.Add(It.IsAny<Setup>()), Times.Once);
        _context.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

