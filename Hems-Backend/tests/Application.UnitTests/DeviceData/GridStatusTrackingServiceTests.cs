using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Hems_Backend.Application.UnitTests.DeviceData;

[TestFixture]
public class GridStatusTrackingServiceTests
{
    private GridStatusTrackingService _service;
    private Mock<ILogger<GridStatusTrackingService>> _logger;

    [SetUp]
    public void SetUp()
    {
        _logger = new Mock<ILogger<GridStatusTrackingService>>();
        _service = new GridStatusTrackingService(_logger.Object);
    }

    #region First Message Tests

    [Test]
    public void UpdateGridStatus_FirstMessage_GridConnected_ShouldNotSendNotification()
    {
        // Arrange
        var deviceId = Guid.NewGuid();

        // Act
        var result = _service.UpdateGridStatus(deviceId, gridConnected: true);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.PreviousStatus, Is.EqualTo(GridConnectionStatus.Unknown));
        Assert.That(result.CurrentStatus, Is.EqualTo(GridConnectionStatus.Connected));
        Assert.That(result.StatusChanged, Is.True);
        Assert.That(result.ShouldSendNotification, Is.False, "First message with grid connected should NOT send notification");
        Assert.That(result.NotificationCode, Is.Null);
    }

    [Test]
    public void UpdateGridStatus_FirstMessage_GridDisconnected_ShouldSendOutageNotification()
    {
        // Arrange
        var deviceId = Guid.NewGuid();

        // Act
        var result = _service.UpdateGridStatus(deviceId, gridConnected: false);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.PreviousStatus, Is.EqualTo(GridConnectionStatus.Unknown));
        Assert.That(result.CurrentStatus, Is.EqualTo(GridConnectionStatus.Disconnected));
        Assert.That(result.StatusChanged, Is.True);
        Assert.That(result.ShouldSendNotification, Is.True, "First message with grid disconnected SHOULD send notification");
        Assert.That(result.NotificationCode, Is.EqualTo("GRD-WAR-W33"));
    }

    #endregion

    #region State Transition Tests

    [Test]
    public void UpdateGridStatus_ConnectedToDisconnected_ShouldSendOutageNotification()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        _service.UpdateGridStatus(deviceId, gridConnected: true); // Initial: connected

        // Act
        var result = _service.UpdateGridStatus(deviceId, gridConnected: false); // Transition: disconnected

        // Assert
        Assert.That(result.PreviousStatus, Is.EqualTo(GridConnectionStatus.Connected));
        Assert.That(result.CurrentStatus, Is.EqualTo(GridConnectionStatus.Disconnected));
        Assert.That(result.StatusChanged, Is.True);
        Assert.That(result.ShouldSendNotification, Is.True);
        Assert.That(result.NotificationCode, Is.EqualTo("GRD-WAR-W33"));
    }

    [Test]
    public void UpdateGridStatus_DisconnectedToConnected_ShouldSendRestoredNotification()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var outageStart = DateTime.UtcNow.AddMinutes(-5);
        _service.UpdateGridStatus(deviceId, gridConnected: false, timestampUtc: outageStart); // Initial: disconnected

        // Act
        var restoreTime = DateTime.UtcNow;
        var result = _service.UpdateGridStatus(deviceId, gridConnected: true, timestampUtc: restoreTime); // Restored

        // Assert
        Assert.That(result.PreviousStatus, Is.EqualTo(GridConnectionStatus.Disconnected));
        Assert.That(result.CurrentStatus, Is.EqualTo(GridConnectionStatus.Connected));
        Assert.That(result.StatusChanged, Is.True);
        Assert.That(result.ShouldSendNotification, Is.True);
        Assert.That(result.NotificationCode, Is.EqualTo("GRD-WAR-W34"));
        Assert.That(result.OutageDuration, Is.Not.Null);
        Assert.That(result.OutageDuration!.Value.TotalMinutes, Is.GreaterThan(4).And.LessThan(6));
    }

    [Test]
    public void UpdateGridStatus_DisconnectedToDisconnected_ShouldNotSendNotification()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        _service.UpdateGridStatus(deviceId, gridConnected: false); // Initial: disconnected

        // Act - subsequent messages during blackout
        var result = _service.UpdateGridStatus(deviceId, gridConnected: false);

        // Assert
        Assert.That(result.PreviousStatus, Is.EqualTo(GridConnectionStatus.Disconnected));
        Assert.That(result.CurrentStatus, Is.EqualTo(GridConnectionStatus.Disconnected));
        Assert.That(result.StatusChanged, Is.False);
        Assert.That(result.ShouldSendNotification, Is.False, "Repeated disconnected messages should NOT send notification");
    }

    [Test]
    public void UpdateGridStatus_ConnectedToConnected_ShouldNotSendNotification()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        _service.UpdateGridStatus(deviceId, gridConnected: true); // Initial: connected

        // Act
        var result = _service.UpdateGridStatus(deviceId, gridConnected: true);

        // Assert
        Assert.That(result.PreviousStatus, Is.EqualTo(GridConnectionStatus.Connected));
        Assert.That(result.CurrentStatus, Is.EqualTo(GridConnectionStatus.Connected));
        Assert.That(result.StatusChanged, Is.False);
        Assert.That(result.ShouldSendNotification, Is.False);
    }

    #endregion

    #region Outage Duration Tracking

    [Test]
    public void UpdateGridStatus_OutageAndRestore_ShouldTrackOutageDuration()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var outageStart = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var restoreTime = new DateTime(2025, 1, 1, 13, 30, 0, DateTimeKind.Utc);

        _service.UpdateGridStatus(deviceId, gridConnected: true, timestampUtc: outageStart.AddMinutes(-10)); // Connected
        _service.UpdateGridStatus(deviceId, gridConnected: false, timestampUtc: outageStart); // Outage starts

        // Act
        var result = _service.UpdateGridStatus(deviceId, gridConnected: true, timestampUtc: restoreTime); // Restore

        // Assert
        Assert.That(result.OutageDuration, Is.Not.Null);
        Assert.That(result.OutageDuration!.Value, Is.EqualTo(TimeSpan.FromMinutes(90)));
    }

    [Test]
    public void UpdateGridStatus_OutageStartTime_ShouldBeSetOnDisconnect()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var outageTime = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        _service.UpdateGridStatus(deviceId, gridConnected: true); // Connected
        _service.UpdateGridStatus(deviceId, gridConnected: false, timestampUtc: outageTime); // Outage

        // Act
        var state = _service.GetGridStatus(deviceId);

        // Assert
        Assert.That(state, Is.Not.Null);
        Assert.That(state!.OutageStartTime, Is.EqualTo(outageTime));
        Assert.That(state.Status, Is.EqualTo(GridConnectionStatus.Disconnected));
    }

    [Test]
    public void UpdateGridStatus_OutageStartTime_ShouldBeClearedOnRestore()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        _service.UpdateGridStatus(deviceId, gridConnected: false); // Outage
        _service.UpdateGridStatus(deviceId, gridConnected: true);  // Restore

        // Act
        var state = _service.GetGridStatus(deviceId);

        // Assert
        Assert.That(state, Is.Not.Null);
        Assert.That(state!.OutageStartTime, Is.Null);
        Assert.That(state.Status, Is.EqualTo(GridConnectionStatus.Connected));
    }

    #endregion

    #region Voltage Tracking

    [Test]
    public void UpdateGridStatus_ShouldTrackPhaseVoltages()
    {
        // Arrange
        var deviceId = Guid.NewGuid();

        // Act
        _service.UpdateGridStatus(deviceId, gridConnected: true,
            phaseAVoltageV: 230.5, phaseBVoltageV: 229.8, phaseCVoltageV: 231.2);

        // Assert
        var state = _service.GetGridStatus(deviceId);
        Assert.That(state, Is.Not.Null);
        Assert.That(state!.PhaseAVoltageV, Is.EqualTo(230.5));
        Assert.That(state.PhaseBVoltageV, Is.EqualTo(229.8));
        Assert.That(state.PhaseCVoltageV, Is.EqualTo(231.2));
    }

    #endregion

    #region Full Blackout Scenario (End-to-End)

    [Test]
    public void FullBlackoutScenario_ShouldWorkCorrectly()
    {
        // Simulates: device connected → blackout detected → repeated blackout messages → power restored
        var deviceId = Guid.NewGuid();
        var baseTime = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        // Step 1: Device first reports connected
        var result1 = _service.UpdateGridStatus(deviceId, gridConnected: true,
            phaseAVoltageV: 230, phaseBVoltageV: 229, phaseCVoltageV: 231,
            timestampUtc: baseTime);
        Assert.That(result1.ShouldSendNotification, Is.False, "First connected message: no notification");

        // Step 2: Blackout detected (gridConnected = false)
        var result2 = _service.UpdateGridStatus(deviceId, gridConnected: false,
            phaseAVoltageV: 0, phaseBVoltageV: 0, phaseCVoltageV: 0,
            timestampUtc: baseTime.AddMinutes(1));
        Assert.That(result2.ShouldSendNotification, Is.True, "First blackout: should notify");
        Assert.That(result2.NotificationCode, Is.EqualTo("GRD-WAR-W33"));

        // Step 3: Repeated blackout messages every minute - should NOT re-notify
        for (int i = 2; i <= 5; i++)
        {
            var resultN = _service.UpdateGridStatus(deviceId, gridConnected: false,
                phaseAVoltageV: 0, phaseBVoltageV: 0, phaseCVoltageV: 0,
                timestampUtc: baseTime.AddMinutes(i));
            Assert.That(resultN.ShouldSendNotification, Is.False,
                $"Repeated blackout message #{i}: should NOT notify again");
            Assert.That(resultN.StatusChanged, Is.False);
        }

        // Step 4: Power restored
        var resultRestore = _service.UpdateGridStatus(deviceId, gridConnected: true,
            phaseAVoltageV: 230, phaseBVoltageV: 229, phaseCVoltageV: 231,
            timestampUtc: baseTime.AddMinutes(6));
        Assert.That(resultRestore.ShouldSendNotification, Is.True, "Power restored: should notify");
        Assert.That(resultRestore.NotificationCode, Is.EqualTo("GRD-WAR-W34"));
        Assert.That(resultRestore.OutageDuration, Is.Not.Null);
        Assert.That(resultRestore.OutageDuration!.Value, Is.EqualTo(TimeSpan.FromMinutes(5)));

        // Step 5: Continued connected messages - no notification
        var resultContinued = _service.UpdateGridStatus(deviceId, gridConnected: true,
            phaseAVoltageV: 230, phaseBVoltageV: 229, phaseCVoltageV: 231,
            timestampUtc: baseTime.AddMinutes(7));
        Assert.That(resultContinued.ShouldSendNotification, Is.False, "Continued connected: no notification");
    }

    #endregion

    #region Multi-Device Isolation

    [Test]
    public void UpdateGridStatus_MultipleDevices_ShouldTrackIndependently()
    {
        // Arrange
        var device1 = Guid.NewGuid();
        var device2 = Guid.NewGuid();

        // Act: device1 goes offline, device2 stays online
        _service.UpdateGridStatus(device1, gridConnected: true);
        _service.UpdateGridStatus(device2, gridConnected: true);
        var result1 = _service.UpdateGridStatus(device1, gridConnected: false);
        var result2 = _service.UpdateGridStatus(device2, gridConnected: true);

        // Assert
        Assert.That(result1.ShouldSendNotification, Is.True, "Device1 should get outage notification");
        Assert.That(result2.ShouldSendNotification, Is.False, "Device2 should NOT get notification");

        Assert.That(_service.GetDevicesInOutageMode(), Contains.Item(device1));
        Assert.That(_service.GetDevicesInOutageMode(), Does.Not.Contain(device2));
    }

    #endregion

    #region GetDevicesNeedingRpcFallback

    [Test]
    public void GetDevicesNeedingRpcFallback_ShouldReturnDevicesWithStaleMessages()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var staleTime = DateTime.UtcNow.AddSeconds(-120); // 2 minutes ago

        _service.UpdateGridStatus(deviceId, gridConnected: false, timestampUtc: staleTime);

        // Act
        var result = _service.GetDevicesNeedingRpcFallback(TimeSpan.FromSeconds(90));

        // Assert
        Assert.That(result, Contains.Item(deviceId));
    }

    [Test]
    public void GetDevicesNeedingRpcFallback_ShouldNotReturnDevicesWithRecentMessages()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        _service.UpdateGridStatus(deviceId, gridConnected: false, timestampUtc: DateTime.UtcNow);

        // Act
        var result = _service.GetDevicesNeedingRpcFallback(TimeSpan.FromSeconds(90));

        // Assert
        Assert.That(result, Does.Not.Contain(deviceId));
    }

    [Test]
    public void GetDevicesNeedingRpcFallback_ShouldNotReturnConnectedDevices()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var staleTime = DateTime.UtcNow.AddSeconds(-120);
        _service.UpdateGridStatus(deviceId, gridConnected: true, timestampUtc: staleTime);

        // Act
        var result = _service.GetDevicesNeedingRpcFallback(TimeSpan.FromSeconds(90));

        // Assert
        Assert.That(result, Does.Not.Contain(deviceId), "Connected devices should not need RPC fallback");
    }

    #endregion

    #region RemoveDevice

    [Test]
    public void RemoveDevice_ShouldRemoveDeviceFromTracking()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        _service.UpdateGridStatus(deviceId, gridConnected: true);

        // Act
        var removed = _service.RemoveDevice(deviceId);

        // Assert
        Assert.That(removed, Is.True);
        Assert.That(_service.GetGridStatus(deviceId), Is.Null);
        Assert.That(_service.GetTrackedDeviceCount(), Is.EqualTo(0));
    }

    #endregion
}
