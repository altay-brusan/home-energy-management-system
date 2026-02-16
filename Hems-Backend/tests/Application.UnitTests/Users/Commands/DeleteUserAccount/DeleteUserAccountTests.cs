using Hems_Backend.Application.Common.Exceptions;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.Users.Commands.DeleteUserAccount;
using Hems_Backend.Domain.Constants;
using Hems_Backend.Domain.Entities;
using Hems_Backend.Domain.Enums;
using MockQueryable.Moq;
using Moq;
using NUnit.Framework;

namespace Hems_Backend.Application.UnitTests.Users.Commands.DeleteUserAccount;

[TestFixture]
public class DeleteUserAccountTests
{
    private Mock<IApplicationDbContext> _context = null!;
    private Mock<IUser> _user = null!;
    private Mock<IIdentityService> _identityService = null!;
    private DeleteUserAccountCommandHandler _handler = null!;

    private const string TestUserId = "test-user-id";

    [SetUp]
    public void SetUp()
    {
        _context = new Mock<IApplicationDbContext>();
        _user = new Mock<IUser>();
        _identityService = new Mock<IIdentityService>();

        _user.Setup(x => x.Id).Returns(TestUserId);
        _identityService.Setup(x => x.IsInRoleAsync(It.IsAny<string>(), Roles.Administrator))
            .ReturnsAsync(false);
        _identityService.Setup(x => x.IsUserDemoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _identityService.Setup(x => x.SoftDeleteUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        _context.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new DeleteUserAccountCommandHandler(_context.Object, _user.Object, _identityService.Object);
    }

    private void SetupDbSets(
        List<Site>? sites = null,
        List<UserSite>? userSites = null,
        List<Setup>? setups = null,
        List<SetupDevice>? setupDevices = null,
        List<Invitation>? invitations = null,
        List<Notification>? notifications = null)
    {
        sites ??= new List<Site>();
        userSites ??= new List<UserSite>();
        setups ??= new List<Setup>();
        setupDevices ??= new List<SetupDevice>();
        invitations ??= new List<Invitation>();
        notifications ??= new List<Notification>();

        _context.Setup(x => x.Sites).Returns(sites.AsQueryable().BuildMockDbSet().Object);
        _context.Setup(x => x.UserSites).Returns(userSites.AsQueryable().BuildMockDbSet().Object);
        _context.Setup(x => x.Setups).Returns(setups.AsQueryable().BuildMockDbSet().Object);
        _context.Setup(x => x.SetupDevices).Returns(setupDevices.AsQueryable().BuildMockDbSet().Object);
        _context.Setup(x => x.Invitations).Returns(invitations.AsQueryable().BuildMockDbSet().Object);
        _context.Setup(x => x.Notifications).Returns(notifications.AsQueryable().BuildMockDbSet().Object);
    }

    [Test]
    public void Handle_UnauthenticatedUser_ShouldThrowUnauthorized()
    {
        // Arrange
        _user.Setup(x => x.Id).Returns((string?)null);
        _handler = new DeleteUserAccountCommandHandler(_context.Object, _user.Object, _identityService.Object);

        // Act & Assert
        var ex = Assert.ThrowsAsync<AppException>(
            () => _handler.Handle(new DeleteUserAccountCommand(), CancellationToken.None));

        Assert.That(ex!.Code, Is.EqualTo(MessageCodes.AUTH_USER_NOT_AUTHENTICATED));
    }

    [Test]
    public void Handle_AdminUser_ShouldThrowAdminCannotDelete()
    {
        // Arrange
        _identityService.Setup(x => x.IsInRoleAsync(TestUserId, Roles.Administrator))
            .ReturnsAsync(true);
        SetupDbSets();

        // Act & Assert
        var ex = Assert.ThrowsAsync<AppException>(
            () => _handler.Handle(new DeleteUserAccountCommand(), CancellationToken.None));

        Assert.That(ex!.Code, Is.EqualTo(MessageCodes.USER_ADMIN_CANNOT_DELETE));
    }

    [Test]
    public void Handle_DemoUser_ShouldThrowDemoCannotDelete()
    {
        // Arrange
        _identityService.Setup(x => x.IsUserDemoAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        SetupDbSets();

        // Act & Assert
        var ex = Assert.ThrowsAsync<AppException>(
            () => _handler.Handle(new DeleteUserAccountCommand(), CancellationToken.None));

        Assert.That(ex!.Code, Is.EqualTo(MessageCodes.USER_DEMO_CANNOT_DELETE));
    }

    [Test]
    public void Handle_OwnerWithActiveShares_ShouldThrowCannotDeleteWithActiveShares()
    {
        // Arrange
        var siteId = Guid.NewGuid();
        var sites = new List<Site>
        {
            new Site { Id = siteId, UserId = TestUserId, IsDeleted = false, Name = "My Site" }
        };
        var userSites = new List<UserSite>
        {
            new UserSite { Id = Guid.NewGuid(), SiteId = siteId, UserId = TestUserId, Role = UserSiteRole.Owner },
            new UserSite { Id = Guid.NewGuid(), SiteId = siteId, UserId = "guest-user-id", Role = UserSiteRole.Guest }
        };

        SetupDbSets(sites: sites, userSites: userSites);

        // Act & Assert
        var ex = Assert.ThrowsAsync<AppException>(
            () => _handler.Handle(new DeleteUserAccountCommand(), CancellationToken.None));

        Assert.That(ex!.Code, Is.EqualTo(MessageCodes.USER_CANNOT_DELETE_WITH_ACTIVE_SHARES));
        Assert.That(ex.Params, Is.Not.Null);
        Assert.That(ex.Params!["sharedSiteCount"], Is.EqualTo(1));
    }

    [Test]
    public async Task Handle_GuestOnlyUser_ShouldRemoveUserSitesAndSoftDelete()
    {
        // Arrange - user is only a guest on another owner's site
        var otherSiteId = Guid.NewGuid();
        var userSites = new List<UserSite>
        {
            new UserSite { Id = Guid.NewGuid(), SiteId = otherSiteId, UserId = TestUserId, Role = UserSiteRole.Guest }
        };

        SetupDbSets(userSites: userSites);

        // Act
        await _handler.Handle(new DeleteUserAccountCommand(), CancellationToken.None);

        // Assert
        _identityService.Verify(x => x.SoftDeleteUserAsync(TestUserId, It.IsAny<CancellationToken>()), Times.Once);
        _context.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_OwnerWithoutShares_ShouldHardDeleteSetupChainAndSoftDeleteSite()
    {
        // Arrange
        var siteId = Guid.NewGuid();
        var setupId = Guid.NewGuid();
        var setupDeviceId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();

        var setupDevice = new SetupDevice { Id = setupDeviceId, SetupId = setupId, DeviceId = deviceId };
        var setup = new Setup { Id = setupId, SiteId = siteId, SetupDevice = setupDevice };
        var site = new Site { Id = siteId, UserId = TestUserId, IsDeleted = false, Name = "My Site" };

        var sites = new List<Site> { site };
        var setups = new List<Setup> { setup };
        var setupDevices = new List<SetupDevice> { setupDevice };
        var userSites = new List<UserSite>
        {
            new UserSite { Id = Guid.NewGuid(), SiteId = siteId, UserId = TestUserId, Role = UserSiteRole.Owner }
        };

        SetupDbSets(sites: sites, userSites: userSites, setups: setups, setupDevices: setupDevices);

        // Act
        await _handler.Handle(new DeleteUserAccountCommand(), CancellationToken.None);

        // Assert — SetupDevice and Setup are hard-deleted
        _context.Verify(x => x.SetupDevices.Remove(It.Is<SetupDevice>(sd => sd.Id == setupDeviceId)), Times.Once);
        _context.Verify(x => x.Setups.Remove(It.Is<Setup>(s => s.Id == setupId)), Times.Once);

        // Assert — Site is soft-deleted
        Assert.That(site.IsDeleted, Is.True);

        // Assert — user is soft-deleted
        _identityService.Verify(x => x.SoftDeleteUserAsync(TestUserId, It.IsAny<CancellationToken>()), Times.Once);
        _context.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_OwnerWithoutShares_DeviceIsNotDeleted()
    {
        // Arrange - device should be preserved when owner deletes
        var siteId = Guid.NewGuid();
        var setupId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var device = new Device { Id = deviceId, Manufacturer = "Test" };

        var setupDevice = new SetupDevice { Id = Guid.NewGuid(), SetupId = setupId, DeviceId = deviceId, Device = device };
        var setup = new Setup { Id = setupId, SiteId = siteId, SetupDevice = setupDevice };
        var site = new Site { Id = siteId, UserId = TestUserId, IsDeleted = false, Name = "My Site" };

        var sites = new List<Site> { site };
        var setups = new List<Setup> { setup };
        var devices = new List<Device> { device };

        SetupDbSets(sites: sites, setups: setups, setupDevices: new List<SetupDevice> { setupDevice },
            userSites: new List<UserSite>
            {
                new UserSite { Id = Guid.NewGuid(), SiteId = siteId, UserId = TestUserId, Role = UserSiteRole.Owner }
            });

        var mockDeviceDbSet = devices.AsQueryable().BuildMockDbSet();
        _context.Setup(x => x.Devices).Returns(mockDeviceDbSet.Object);

        // Act
        await _handler.Handle(new DeleteUserAccountCommand(), CancellationToken.None);

        // Assert — Device.Remove is never called
        _context.Verify(x => x.Devices.Remove(It.IsAny<Device>()), Times.Never);
    }

    [Test]
    public async Task Handle_InvitationsAreCleanedUp_BothSentAndReceived()
    {
        // Arrange
        var siteId = Guid.NewGuid();
        var site = new Site { Id = siteId, UserId = TestUserId, IsDeleted = false, Name = "My Site" };

        // Invitation sent by this user (for their owned site)
        var sentInvitation = new Invitation
        {
            Id = Guid.NewGuid(), SiteId = siteId, Email = "guest@test.com",
            InvitedByUserId = TestUserId, Status = InvitationStatus.Pending, Token = "token1"
        };

        // Invitation received by this user (for another site)
        var receivedInvitation = new Invitation
        {
            Id = Guid.NewGuid(), SiteId = Guid.NewGuid(), Email = "test@test.com",
            InvitedUserId = TestUserId, InvitedByUserId = "other-user", Status = InvitationStatus.Pending, Token = "token2"
        };

        SetupDbSets(
            sites: new List<Site> { site },
            userSites: new List<UserSite>
            {
                new UserSite { Id = Guid.NewGuid(), SiteId = siteId, UserId = TestUserId, Role = UserSiteRole.Owner }
            },
            invitations: new List<Invitation> { sentInvitation, receivedInvitation });

        // Act
        await _handler.Handle(new DeleteUserAccountCommand(), CancellationToken.None);

        // Assert — both sent (site) and received invitations are removed
        _context.Verify(x => x.Invitations.RemoveRange(It.Is<IEnumerable<Invitation>>(
            invs => invs.Any(i => i.Id == sentInvitation.Id))), Times.Once);
        _context.Verify(x => x.Invitations.RemoveRange(It.Is<IEnumerable<Invitation>>(
            invs => invs.Any(i => i.Id == receivedInvitation.Id))), Times.Once);
    }

    [Test]
    public async Task Handle_NotificationsAreCleanedUp()
    {
        // Arrange
        var notification = new Notification
        {
            Id = Guid.NewGuid(), UserId = TestUserId, Email = "test@test.com",
            InvitationId = Guid.NewGuid(), Message = "You have an invitation"
        };

        SetupDbSets(notifications: new List<Notification> { notification });

        // Act
        await _handler.Handle(new DeleteUserAccountCommand(), CancellationToken.None);

        // Assert
        _context.Verify(x => x.Notifications.RemoveRange(It.Is<IEnumerable<Notification>>(
            ns => ns.Any(n => n.Id == notification.Id))), Times.Once);
    }

    [Test]
    public async Task Handle_UserWithMultipleOwnedSites_AllProcessed()
    {
        // Arrange
        var siteId1 = Guid.NewGuid();
        var siteId2 = Guid.NewGuid();
        var site1 = new Site { Id = siteId1, UserId = TestUserId, IsDeleted = false, Name = "Site 1" };
        var site2 = new Site { Id = siteId2, UserId = TestUserId, IsDeleted = false, Name = "Site 2" };

        var setup1 = new Setup { Id = Guid.NewGuid(), SiteId = siteId1, SetupDevice = null };
        var setupDevice2 = new SetupDevice { Id = Guid.NewGuid(), SetupId = Guid.NewGuid(), DeviceId = Guid.NewGuid() };
        var setup2 = new Setup { Id = Guid.NewGuid(), SiteId = siteId2, SetupDevice = setupDevice2 };
        setupDevice2.SetupId = setup2.Id;

        SetupDbSets(
            sites: new List<Site> { site1, site2 },
            setups: new List<Setup> { setup1, setup2 },
            setupDevices: new List<SetupDevice> { setupDevice2 },
            userSites: new List<UserSite>
            {
                new UserSite { Id = Guid.NewGuid(), SiteId = siteId1, UserId = TestUserId, Role = UserSiteRole.Owner },
                new UserSite { Id = Guid.NewGuid(), SiteId = siteId2, UserId = TestUserId, Role = UserSiteRole.Owner }
            });

        // Act
        await _handler.Handle(new DeleteUserAccountCommand(), CancellationToken.None);

        // Assert — both sites are soft-deleted
        Assert.That(site1.IsDeleted, Is.True);
        Assert.That(site2.IsDeleted, Is.True);

        // Assert — setup chain cleaned up
        _context.Verify(x => x.Setups.Remove(It.Is<Setup>(s => s.SiteId == siteId1)), Times.Once);
        _context.Verify(x => x.Setups.Remove(It.Is<Setup>(s => s.SiteId == siteId2)), Times.Once);
        _context.Verify(x => x.SetupDevices.Remove(It.Is<SetupDevice>(sd => sd.Id == setupDevice2.Id)), Times.Once);

        _identityService.Verify(x => x.SoftDeleteUserAsync(TestUserId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_UserIsBothOwnerAndGuest_BothSidesCleaned()
    {
        // Arrange
        var ownedSiteId = Guid.NewGuid();
        var guestSiteId = Guid.NewGuid();
        var ownedSite = new Site { Id = ownedSiteId, UserId = TestUserId, IsDeleted = false, Name = "Owned Site" };

        // The guest site belongs to another user, so it won't be in the owned sites query
        var userSites = new List<UserSite>
        {
            new UserSite { Id = Guid.NewGuid(), SiteId = ownedSiteId, UserId = TestUserId, Role = UserSiteRole.Owner },
            new UserSite { Id = Guid.NewGuid(), SiteId = guestSiteId, UserId = TestUserId, Role = UserSiteRole.Guest }
        };

        SetupDbSets(
            sites: new List<Site> { ownedSite },
            userSites: userSites);

        // Act
        await _handler.Handle(new DeleteUserAccountCommand(), CancellationToken.None);

        // Assert — owned site is soft-deleted
        Assert.That(ownedSite.IsDeleted, Is.True);

        // Assert — all UserSite records (both owner and guest) are removed
        _context.Verify(x => x.UserSites.RemoveRange(It.Is<IEnumerable<UserSite>>(
            us => us.Count() == 2)), Times.Once);

        _identityService.Verify(x => x.SoftDeleteUserAsync(TestUserId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_UserWithNoSitesOrData_ShouldSoftDeleteCleanly()
    {
        // Arrange — a user with absolutely nothing
        SetupDbSets();

        // Act
        await _handler.Handle(new DeleteUserAccountCommand(), CancellationToken.None);

        // Assert
        _identityService.Verify(x => x.SoftDeleteUserAsync(TestUserId, It.IsAny<CancellationToken>()), Times.Once);
        _context.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
