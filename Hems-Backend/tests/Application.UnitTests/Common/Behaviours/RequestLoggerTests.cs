using Hems_Backend.Application.Common.Behaviours;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Sites.Commands.CreateSite;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Hems_Backend.Application.UnitTests.Common.Behaviours;

public class RequestLoggerTests
{
    private Mock<ILogger<CreateSiteCommand>> _logger = null!;
    private Mock<IUser> _user = null!;
    private Mock<IIdentityService> _identityService = null!;

    [SetUp]
    public void Setup()
    {
        _logger = new Mock<ILogger<CreateSiteCommand>>();
        _user = new Mock<IUser>();
        _identityService = new Mock<IIdentityService>();
    }

    [Test]
    public async Task ShouldCallGetUserNameAsyncOnceIfAuthenticated()
    {
        _user.Setup(x => x.Id).Returns(Guid.NewGuid().ToString());

        var requestLogger = new LoggingBehaviour<CreateSiteCommand>(_logger.Object, _user.Object, _identityService.Object);

        await requestLogger.Process(new CreateSiteCommand { Name = "Test Site" }, new CancellationToken());

        _identityService.Verify(i => i.GetUserNameAsync(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task ShouldNotCallGetUserNameAsyncOnceIfUnauthenticated()
    {
        var requestLogger = new LoggingBehaviour<CreateSiteCommand>(_logger.Object, _user.Object, _identityService.Object);

        await requestLogger.Process(new CreateSiteCommand { Name = "Test Site" }, new CancellationToken());

        _identityService.Verify(i => i.GetUserNameAsync(It.IsAny<string>()), Times.Never);
    }
}
