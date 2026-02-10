using System.Text.Json;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.DeviceData.Commands.ProcessGridStatusTelemetry;
using Hems_Backend.Application.DeviceNotifications.Commands.ProcessDeviceAlarm;
using Hems_Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Hems_Backend.Application.UnitTests.DeviceData;

[TestFixture]
public class ProcessGridStatusTelemetryTests
{
    private Mock<IApplicationDbContext> _context;
    private Mock<IDeviceMappingService> _deviceMapping;
    private Mock<IDeviceActivityService> _activityService;
    private Mock<IDemoDataService> _demoDataService;
    private Mock<IGridStatusTrackingService> _gridStatusTracking;
    private Mock<IAlarmNotificationSender> _notificationSender;
    private Mock<ILogger<ProcessGridStatusTelemetryCommandHandler>> _logger;
    private ProcessGridStatusTelemetryCommandHandler _handler;

    [SetUp]
    public void SetUp()
    {
        _context = new Mock<IApplicationDbContext>();
        _deviceMapping = new Mock<IDeviceMappingService>();
        _activityService = new Mock<IDeviceActivityService>();
        _demoDataService = new Mock<IDemoDataService>();
        _gridStatusTracking = new Mock<IGridStatusTrackingService>();
        _notificationSender = new Mock<IAlarmNotificationSender>();
        _logger = new Mock<ILogger<ProcessGridStatusTelemetryCommandHandler>>();

        // Default: not a demo device
        _demoDataService.Setup(x => x.IsDemoDeviceByThingsBoardIdAsync(
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Default: grid status tracking returns no change
        _gridStatusTracking.Setup(x => x.UpdateGridStatus(
            It.IsAny<Guid>(), It.IsAny<bool>(),
            It.IsAny<double?>(), It.IsAny<double?>(), It.IsAny<double?>(),
            It.IsAny<DateTime?>()))
            .Returns(new GridStatusUpdateResult
            {
                Success = true,
                StatusChanged = false,
                ShouldSendNotification = false
            });

        _handler = new ProcessGridStatusTelemetryCommandHandler(
            _context.Object,
            _deviceMapping.Object,
            _activityService.Object,
            _demoDataService.Object,
            _gridStatusTracking.Object,
            _notificationSender.Object,
            _logger.Object);
    }

    #region TryGetGridConnectedValue - Direct static method tests

    [Test]
    public void TryGetGridConnectedValue_BooleanTrue_ShouldReturnTrue()
    {
        var json = JsonDocument.Parse("""{"gridConnected": true}""").RootElement;
        var result = ProcessGridStatusTelemetryCommandHandler.TryGetGridConnectedValue(json, out var value);
        Assert.That(result, Is.True);
        Assert.That(value, Is.True);
    }

    [Test]
    public void TryGetGridConnectedValue_BooleanFalse_ShouldReturnFalse()
    {
        var json = JsonDocument.Parse("""{"gridConnected": false}""").RootElement;
        var result = ProcessGridStatusTelemetryCommandHandler.TryGetGridConnectedValue(json, out var value);
        Assert.That(result, Is.True);
        Assert.That(value, Is.False);
    }

    [Test]
    public void TryGetGridConnectedValue_Integer0_ShouldReturnFalse()
    {
        var json = JsonDocument.Parse("""{"gridConnected": 0}""").RootElement;
        var result = ProcessGridStatusTelemetryCommandHandler.TryGetGridConnectedValue(json, out var value);
        Assert.That(result, Is.True);
        Assert.That(value, Is.False, "Integer 0 should be parsed as false (disconnected)");
    }

    [Test]
    public void TryGetGridConnectedValue_Integer1_ShouldReturnTrue()
    {
        var json = JsonDocument.Parse("""{"gridConnected": 1}""").RootElement;
        var result = ProcessGridStatusTelemetryCommandHandler.TryGetGridConnectedValue(json, out var value);
        Assert.That(result, Is.True);
        Assert.That(value, Is.True, "Integer 1 should be parsed as true (connected)");
    }

    [Test]
    public void TryGetGridConnectedValue_StringTrue_ShouldReturnTrue()
    {
        var json = JsonDocument.Parse("""{"gridConnected": "true"}""").RootElement;
        var result = ProcessGridStatusTelemetryCommandHandler.TryGetGridConnectedValue(json, out var value);
        Assert.That(result, Is.True);
        Assert.That(value, Is.True);
    }

    [Test]
    public void TryGetGridConnectedValue_StringFalse_ShouldReturnFalse()
    {
        var json = JsonDocument.Parse("""{"gridConnected": "false"}""").RootElement;
        var result = ProcessGridStatusTelemetryCommandHandler.TryGetGridConnectedValue(json, out var value);
        Assert.That(result, Is.True);
        Assert.That(value, Is.False, "String 'false' should be parsed as false (disconnected)");
    }

    [Test]
    public void TryGetGridConnectedValue_String0_ShouldReturnFalse()
    {
        var json = JsonDocument.Parse("""{"gridConnected": "0"}""").RootElement;
        var result = ProcessGridStatusTelemetryCommandHandler.TryGetGridConnectedValue(json, out var value);
        Assert.That(result, Is.True);
        Assert.That(value, Is.False);
    }

    [Test]
    public void TryGetGridConnectedValue_String1_ShouldReturnTrue()
    {
        var json = JsonDocument.Parse("""{"gridConnected": "1"}""").RootElement;
        var result = ProcessGridStatusTelemetryCommandHandler.TryGetGridConnectedValue(json, out var value);
        Assert.That(result, Is.True);
        Assert.That(value, Is.True);
    }

    [Test]
    public void TryGetGridConnectedValue_SnakeCase_ShouldWork()
    {
        var json = JsonDocument.Parse("""{"grid_connected": false}""").RootElement;
        var result = ProcessGridStatusTelemetryCommandHandler.TryGetGridConnectedValue(json, out var value);
        Assert.That(result, Is.True, "snake_case 'grid_connected' should be recognized");
        Assert.That(value, Is.False);
    }

    [Test]
    public void TryGetGridConnectedValue_PascalCase_ShouldWork()
    {
        var json = JsonDocument.Parse("""{"GridConnected": false}""").RootElement;
        var result = ProcessGridStatusTelemetryCommandHandler.TryGetGridConnectedValue(json, out var value);
        Assert.That(result, Is.True, "PascalCase 'GridConnected' should be recognized");
        Assert.That(value, Is.False);
    }

    [Test]
    public void TryGetGridConnectedValue_MissingProperty_ShouldReturnFalse()
    {
        var json = JsonDocument.Parse("""{"otherField": true}""").RootElement;
        var result = ProcessGridStatusTelemetryCommandHandler.TryGetGridConnectedValue(json, out _);
        Assert.That(result, Is.False, "Missing gridConnected property should return false");
    }

    #endregion

    #region Handler - Missing deviceId

    [Test]
    public async Task Handle_MissingDeviceId_ShouldReturnError()
    {
        // Arrange
        var body = JsonDocument.Parse("""{"data": {"gridConnected": false}}""").RootElement;
        var command = new ProcessGridStatusTelemetryCommand { RawBody = body };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Does.Contain("deviceId"));
    }

    [Test]
    public async Task Handle_InvalidDeviceIdFormat_ShouldReturnError()
    {
        // Arrange
        var body = JsonDocument.Parse("""{"deviceId": "not-a-guid", "data": {"gridConnected": false}}""").RootElement;
        var command = new ProcessGridStatusTelemetryCommand { RawBody = body };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Does.Contain("deviceId"));
    }

    #endregion

    #region Handler - Missing data object

    [Test]
    public async Task Handle_MissingDataObject_ShouldReturnError()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var body = JsonDocument.Parse($@"{{""deviceId"": ""{deviceId}""}}").RootElement;
        var command = new ProcessGridStatusTelemetryCommand { RawBody = body };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Does.Contain("data"));
    }

    #endregion

    #region Handler - Demo device

    [Test]
    public async Task Handle_DemoDevice_ShouldSkipProcessing()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        _demoDataService.Setup(x => x.IsDemoDeviceByThingsBoardIdAsync(
            deviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var body = JsonDocument.Parse($@"{{""deviceId"": ""{deviceId}"", ""data"": {{""gridConnected"": false}}}}").RootElement;
        var command = new ProcessGridStatusTelemetryCommand { RawBody = body };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Message, Does.Contain("Demo"));
        Assert.That(result.StatusChanged, Is.False);
        _gridStatusTracking.Verify(x => x.UpdateGridStatus(
            It.IsAny<Guid>(), It.IsAny<bool>(),
            It.IsAny<double?>(), It.IsAny<double?>(), It.IsAny<double?>(),
            It.IsAny<DateTime?>()), Times.Never);
    }

    #endregion

    #region Handler - gridConnected=false should pass false to tracking service

    [Test]
    public async Task Handle_GridConnectedFalseBoolean_ShouldPassFalseToTrackingService()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var body = JsonDocument.Parse($@"{{""deviceId"": ""{deviceId}"", ""data"": {{""gridConnected"": false}}}}").RootElement;
        var command = new ProcessGridStatusTelemetryCommand { RawBody = body };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _gridStatusTracking.Verify(x => x.UpdateGridStatus(
            deviceId, false, null, null, null, It.IsAny<DateTime?>()), Times.Once,
            "gridConnected=false (boolean) should pass false to tracking service");
    }

    [Test]
    public async Task Handle_GridConnectedFalseAsInteger0_ShouldPassFalseToTrackingService()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var body = JsonDocument.Parse($@"{{""deviceId"": ""{deviceId}"", ""data"": {{""gridConnected"": 0}}}}").RootElement;
        var command = new ProcessGridStatusTelemetryCommand { RawBody = body };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _gridStatusTracking.Verify(x => x.UpdateGridStatus(
            deviceId, false, null, null, null, It.IsAny<DateTime?>()), Times.Once,
            "gridConnected=0 (integer) should pass false to tracking service");
    }

    [Test]
    public async Task Handle_GridConnectedFalseAsString_ShouldPassFalseToTrackingService()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var body = JsonDocument.Parse($@"{{""deviceId"": ""{deviceId}"", ""data"": {{""gridConnected"": ""false""}}}}").RootElement;
        var command = new ProcessGridStatusTelemetryCommand { RawBody = body };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _gridStatusTracking.Verify(x => x.UpdateGridStatus(
            deviceId, false, null, null, null, It.IsAny<DateTime?>()), Times.Once,
            "gridConnected='false' (string) should pass false to tracking service");
    }

    [Test]
    public async Task Handle_GridConnectedSnakeCase_ShouldPassCorrectValue()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var body = JsonDocument.Parse($@"{{""deviceId"": ""{deviceId}"", ""data"": {{""grid_connected"": false}}}}").RootElement;
        var command = new ProcessGridStatusTelemetryCommand { RawBody = body };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _gridStatusTracking.Verify(x => x.UpdateGridStatus(
            deviceId, false, null, null, null, It.IsAny<DateTime?>()), Times.Once,
            "grid_connected (snake_case) should be recognized and pass false");
    }

    [Test]
    public async Task Handle_MissingGridConnected_ShouldDefaultToTrue()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var body = JsonDocument.Parse($@"{{""deviceId"": ""{deviceId}"", ""data"": {{""phaseAVoltageV"": 230}}}}").RootElement;
        var command = new ProcessGridStatusTelemetryCommand { RawBody = body };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _gridStatusTracking.Verify(x => x.UpdateGridStatus(
            deviceId, true, 230.0, null, null, It.IsAny<DateTime?>()), Times.Once,
            "Missing gridConnected should default to true (connected)");
    }

    #endregion

    #region Handler - Voltage parsing

    [Test]
    public async Task Handle_WithPhaseVoltages_ShouldPassValuesToTrackingService()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var body = JsonDocument.Parse($@"{{
            ""deviceId"": ""{deviceId}"",
            ""data"": {{
                ""gridConnected"": true,
                ""phaseAVoltageV"": 230.5,
                ""phaseBVoltageV"": 229.8,
                ""phaseCVoltageV"": 231.2
            }}
        }}").RootElement;
        var command = new ProcessGridStatusTelemetryCommand { RawBody = body };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _gridStatusTracking.Verify(x => x.UpdateGridStatus(
            deviceId, true, 230.5, 229.8, 231.2, It.IsAny<DateTime?>()), Times.Once);
    }

    #endregion

    #region Handler - Notification sending

    [Test]
    public async Task Handle_WhenStatusChangedAndNotificationNeeded_ShouldCreateNotification()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var internalDeviceId = Guid.NewGuid();
        var siteId = Guid.NewGuid();
        var setupDeviceId = Guid.NewGuid();
        var notificationTypeId = Guid.NewGuid();

        _gridStatusTracking.Setup(x => x.UpdateGridStatus(
            It.IsAny<Guid>(), It.IsAny<bool>(),
            It.IsAny<double?>(), It.IsAny<double?>(), It.IsAny<double?>(),
            It.IsAny<DateTime?>()))
            .Returns(new GridStatusUpdateResult
            {
                Success = true,
                StatusChanged = true,
                ShouldSendNotification = true,
                NotificationCode = "GRD-WAR-W33",
                CurrentStatus = GridConnectionStatus.Disconnected
            });

        _deviceMapping.Setup(x => x.GetDeviceIdAsync(deviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(internalDeviceId);

        // Mock device with setup and site
        var site = new Site { Id = siteId };
        var setup = new Setup { SiteId = siteId, Site = site };
        var setupDevice = new SetupDevice { Id = setupDeviceId, Setup = setup };
        var device = new Device
        {
            Id = internalDeviceId,
            SetupDevice = setupDevice
        };

        var mockDevices = new List<Device> { device }.AsQueryable();
        var mockDeviceDbSet = new Mock<DbSet<Device>>();
        mockDeviceDbSet.As<IQueryable<Device>>().Setup(m => m.Provider)
            .Returns(new TestAsyncQueryProvider<Device>(mockDevices.Provider));
        mockDeviceDbSet.As<IQueryable<Device>>().Setup(m => m.Expression).Returns(mockDevices.Expression);
        mockDeviceDbSet.As<IQueryable<Device>>().Setup(m => m.ElementType).Returns(mockDevices.ElementType);
        mockDeviceDbSet.As<IAsyncEnumerable<Device>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<Device>(mockDevices.GetEnumerator()));
        _context.Setup(c => c.Devices).Returns(mockDeviceDbSet.Object);

        // Mock notification type
        var notificationType = new DeviceNotificationType
        {
            Id = notificationTypeId,
            Code = "GRD-WAR-W33",
            Message = "Grid power outage detected",
            Severity = "WARNING",
            IsActive = true
        };
        var mockNotificationTypes = new List<DeviceNotificationType> { notificationType }.AsQueryable();
        var mockNtDbSet = new Mock<DbSet<DeviceNotificationType>>();
        mockNtDbSet.As<IQueryable<DeviceNotificationType>>().Setup(m => m.Provider)
            .Returns(new TestAsyncQueryProvider<DeviceNotificationType>(mockNotificationTypes.Provider));
        mockNtDbSet.As<IQueryable<DeviceNotificationType>>().Setup(m => m.Expression).Returns(mockNotificationTypes.Expression);
        mockNtDbSet.As<IQueryable<DeviceNotificationType>>().Setup(m => m.ElementType).Returns(mockNotificationTypes.ElementType);
        mockNtDbSet.As<IAsyncEnumerable<DeviceNotificationType>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<DeviceNotificationType>(mockNotificationTypes.GetEnumerator()));
        _context.Setup(c => c.DeviceNotificationTypes).Returns(mockNtDbSet.Object);

        // Mock DeviceNotifications DbSet
        var mockNotificationsDbSet = new Mock<DbSet<DeviceNotification>>();
        _context.Setup(c => c.DeviceNotifications).Returns(mockNotificationsDbSet.Object);
        _context.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var body = JsonDocument.Parse($@"{{""deviceId"": ""{deviceId}"", ""data"": {{""gridConnected"": false}}}}").RootElement;
        var command = new ProcessGridStatusTelemetryCommand { RawBody = body };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.NotificationSent, Is.True);
        mockNotificationsDbSet.Verify(d => d.Add(It.IsAny<DeviceNotification>()), Times.Once);
        _context.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notificationSender.Verify(x => x.SendNotificationsAsync(
            It.IsAny<List<Guid>>(),
            It.IsAny<List<ProcessedNotificationInfo>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}

#region EF Core Async Test Helpers

internal class TestAsyncQueryProvider<TEntity> : Microsoft.EntityFrameworkCore.Query.IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;
    internal TestAsyncQueryProvider(IQueryProvider inner) => _inner = inner;
    public IQueryable CreateQuery(System.Linq.Expressions.Expression expression) => new TestAsyncEnumerable<TEntity>(expression);
    public IQueryable<TElement> CreateQuery<TElement>(System.Linq.Expressions.Expression expression) => new TestAsyncEnumerable<TElement>(expression);
    public object? Execute(System.Linq.Expressions.Expression expression) => _inner.Execute(expression);
    public TResult Execute<TResult>(System.Linq.Expressions.Expression expression) => _inner.Execute<TResult>(expression);
    public TResult ExecuteAsync<TResult>(System.Linq.Expressions.Expression expression, CancellationToken cancellationToken = default)
    {
        var result = Execute(expression);
        var expectedResultType = typeof(TResult).GetGenericArguments()[0];
        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(expectedResultType)
            .Invoke(null, new[] { result })!;
    }
}

internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
    public TestAsyncEnumerable(System.Linq.Expressions.Expression expression) : base(expression) { }
    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
}

internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;
    public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;
    public T Current => _inner.Current;
    public ValueTask<bool> MoveNextAsync() => new ValueTask<bool>(_inner.MoveNext());
    public ValueTask DisposeAsync() { _inner.Dispose(); return default; }
}

#endregion
