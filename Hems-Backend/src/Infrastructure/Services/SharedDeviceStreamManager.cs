using System.Collections.Concurrent;
using System.Threading.Channels;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.PowerFlow.Common.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hems_Backend.Infrastructure.Services;

/// <summary>
/// Manages shared device data streams with reference counting.
/// Ensures only one polling loop per device regardless of subscriber count.
/// Thread-safe implementation using ConcurrentDictionary and Channels.
/// Uses IServiceScopeFactory to resolve scoped services (like IPowerFlowService) from a singleton.
/// </summary>
public class SharedDeviceStreamManager : ISharedDeviceStreamManager, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SharedDeviceStreamManager> _logger;
    private readonly SharedStreamOptions _options;

    // Main storage: DeviceId -> SharedDeviceStream
    private readonly ConcurrentDictionary<Guid, SharedDeviceStream> _streams = new();

    // Subscription lookup: SubscriptionId -> (DeviceId, Subscription)
    private readonly ConcurrentDictionary<string, (Guid DeviceId, DeviceStreamSubscription Subscription)> _subscriptions = new();

    private bool _disposed;

    public SharedDeviceStreamManager(
        IServiceScopeFactory scopeFactory,
        ILogger<SharedDeviceStreamManager> logger,
        IOptions<SharedStreamOptions>? options = null)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options?.Value ?? new SharedStreamOptions();
    }

    public async Task<IDeviceStreamSubscription> SubscribeAsync(
        Guid deviceId,
        int intervalSeconds = 5,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Clamp interval to configured bounds
        intervalSeconds = Math.Clamp(intervalSeconds,
            _options.MinPollingIntervalSeconds,
            _options.MaxPollingIntervalSeconds);

        var subscriptionId = Guid.NewGuid().ToString();

        _logger.LogInformation(
            "New subscription request for device {DeviceId}, interval {Interval}s, subscriptionId {SubscriptionId}",
            deviceId, intervalSeconds, subscriptionId);

        // Get or create the shared stream for this device
        var stream = _streams.GetOrAdd(deviceId, id => CreateNewStream(id, intervalSeconds));

        // Create subscription with its own channel
        var subscription = new DeviceStreamSubscription(
            deviceId,
            subscriptionId,
            () => UnsubscribeAsync(subscriptionId));

        // Add to stream's subscribers
        stream.AddSubscriber(subscription);

        // Track subscription for lookup
        _subscriptions[subscriptionId] = (deviceId, subscription);

        // Ensure polling is running (pass scope factory for creating scopes in polling loop)
        stream.EnsurePollingStarted(_scopeFactory, _logger, cancellationToken);

        // If we have recent cached data, send it immediately
        if (stream.LastData != null && !stream.IsDataStale(_options.StaleDataThresholdSeconds))
        {
            await subscription.SendDataAsync(stream.LastData);
        }

        _logger.LogInformation(
            "Subscription {SubscriptionId} added for device {DeviceId}. Total subscribers: {Count}",
            subscriptionId, deviceId, stream.SubscriberCount);

        return subscription;
    }

    public Task<bool> UnsubscribeAsync(string subscriptionId)
    {
        if (!_subscriptions.TryRemove(subscriptionId, out var entry))
        {
            _logger.LogWarning("Subscription {SubscriptionId} not found for unsubscribe", subscriptionId);
            return Task.FromResult(false);
        }

        var (deviceId, subscription) = entry;

        // Mark subscription as inactive
        subscription.MarkInactive();

        // Remove from stream
        if (_streams.TryGetValue(deviceId, out var stream))
        {
            stream.RemoveSubscriber(subscriptionId);

            _logger.LogInformation(
                "Subscription {SubscriptionId} removed from device {DeviceId}. Remaining subscribers: {Count}",
                subscriptionId, deviceId, stream.SubscriberCount);

            // If no more subscribers, schedule cleanup
            if (stream.SubscriberCount == 0)
            {
                ScheduleStreamCleanup(deviceId, stream);
            }
        }

        return Task.FromResult(true);
    }

    private SharedDeviceStream CreateNewStream(Guid deviceId, int intervalSeconds)
    {
        _logger.LogInformation(
            "Creating new shared stream for device {DeviceId} with interval {Interval}s",
            deviceId, intervalSeconds);

        return new SharedDeviceStream(deviceId, intervalSeconds);
    }

    private void ScheduleStreamCleanup(Guid deviceId, SharedDeviceStream stream)
    {
        _logger.LogInformation(
            "Scheduling cleanup for device {DeviceId} stream in {Delay}s",
            deviceId, _options.CleanupDelaySeconds);

        // Start cleanup task with delay
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(_options.CleanupDelaySeconds));

            // Double-check no new subscribers joined during delay
            if (stream.SubscriberCount == 0)
            {
                if (_streams.TryRemove(deviceId, out var removedStream))
                {
                    removedStream.Dispose();
                    _logger.LogInformation(
                        "Cleaned up stream for device {DeviceId} after no subscribers",
                        deviceId);
                }
            }
            else
            {
                _logger.LogInformation(
                    "Cleanup cancelled for device {DeviceId} - new subscribers joined",
                    deviceId);
            }
        });
    }

    public SharedStreamStatistics GetStatistics()
    {
        var subscribersPerDevice = _streams.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.SubscriberCount);

        return new SharedStreamStatistics
        {
            ActiveStreamCount = _streams.Count,
            TotalSubscriberCount = _subscriptions.Count,
            SubscribersPerDevice = subscribersPerDevice
        };
    }

    public int GetActiveStreamCount() => _streams.Count;

    public int GetTotalSubscriberCount() => _subscriptions.Count;

    public int GetSubscriberCount(Guid deviceId) =>
        _streams.TryGetValue(deviceId, out var stream) ? stream.SubscriberCount : 0;

    public bool HasActiveSubscribers(Guid deviceId) =>
        _streams.TryGetValue(deviceId, out var stream) && stream.SubscriberCount > 0;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _logger.LogInformation("Disposing SharedDeviceStreamManager with {StreamCount} active streams", _streams.Count);

        foreach (var stream in _streams.Values)
        {
            stream.Dispose();
        }

        _streams.Clear();
        _subscriptions.Clear();
    }
}

/// <summary>
/// Represents a shared stream for a single device.
/// Manages the polling loop and broadcasts data to all subscribers.
/// </summary>
internal class SharedDeviceStream : IDisposable
{
    /// <summary>
    /// Number of consecutive RPC failures before sending zero PowerFlow data.
    /// At 5-second polling interval, 3 failures = 15 seconds before showing "no data".
    /// </summary>
    private const int DeviceOfflineThreshold = 3;

    public Guid DeviceId { get; }
    public int IntervalSeconds { get; }
    public PowerFlowDto? LastData { get; private set; }
    public DateTime? LastUpdated { get; private set; }
    public int SubscriberCount => _subscribers.Count;

    /// <summary>
    /// Indicates if zero data has been sent due to device being offline.
    /// Reset when real data is received again.
    /// </summary>
    public bool IsDeviceOffline { get; private set; }

    private readonly ConcurrentDictionary<string, DeviceStreamSubscription> _subscribers = new();
    private readonly object _pollingLock = new();
    private CancellationTokenSource? _pollingCts;
    private Task? _pollingTask;
    private bool _disposed;

    public SharedDeviceStream(Guid deviceId, int intervalSeconds)
    {
        DeviceId = deviceId;
        IntervalSeconds = intervalSeconds;
    }

    public void AddSubscriber(DeviceStreamSubscription subscription)
    {
        _subscribers[subscription.SubscriptionId] = subscription;
    }

    public void RemoveSubscriber(string subscriptionId)
    {
        _subscribers.TryRemove(subscriptionId, out _);
    }

    public bool IsDataStale(int thresholdSeconds)
    {
        if (LastUpdated == null) return true;
        return (DateTime.UtcNow - LastUpdated.Value).TotalSeconds > thresholdSeconds;
    }

    /// <summary>
    /// Ensures the polling loop is started.
    /// IMPORTANT: The polling loop uses its OWN cancellation token, NOT linked to any subscriber's token.
    /// This prevents the polling from stopping when a single subscriber disconnects.
    /// The polling loop stops only when there are no more subscribers or when disposed.
    /// </summary>
    public void EnsurePollingStarted(
        IServiceScopeFactory scopeFactory,
        ILogger logger,
        CancellationToken externalCancellation)
    {
        // Note: externalCancellation is intentionally NOT used to avoid coupling
        // the polling loop lifetime to a single subscriber's connection.
        // The polling loop manages its own lifecycle based on subscriber count.

        lock (_pollingLock)
        {
            if (_pollingTask != null && !_pollingTask.IsCompleted)
            {
                return; // Already running
            }

            // Create a NEW cancellation token source that's NOT linked to any subscriber
            // This ensures the polling continues even when individual subscribers disconnect
            _pollingCts = new CancellationTokenSource();
            _pollingTask = RunPollingLoopAsync(scopeFactory, logger, _pollingCts.Token);
        }
    }

    private async Task RunPollingLoopAsync(
        IServiceScopeFactory scopeFactory,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Starting polling loop for device {DeviceId} with interval {Interval}s",
            DeviceId, IntervalSeconds);

        var interval = TimeSpan.FromSeconds(IntervalSeconds);
        var consecutiveErrors = 0;
        const int maxConsecutiveErrors = 10;

        while (!cancellationToken.IsCancellationRequested && !_disposed)
        {
            // Check if we still have subscribers
            if (_subscribers.IsEmpty)
            {
                logger.LogInformation(
                    "No subscribers remaining for device {DeviceId}, stopping polling",
                    DeviceId);
                break;
            }

            try
            {
                // Create a scope to resolve scoped services
                using var scope = scopeFactory.CreateScope();
                var powerFlowService = scope.ServiceProvider.GetRequiredService<IPowerFlowService>();

                // Fetch data from device
                var data = await powerFlowService.GetPowerFlowAsync(DeviceId, cancellationToken);

                if (data != null)
                {
                    // Device recovered - reset offline state and error count
                    if (IsDeviceOffline)
                    {
                        logger.LogInformation(
                            "Device {DeviceId} recovered - resuming real PowerFlow data",
                            DeviceId);
                        IsDeviceOffline = false;
                    }

                    LastData = data;
                    LastUpdated = DateTime.UtcNow;
                    consecutiveErrors = 0; // Reset error count on success

                    // Broadcast to all subscribers
                    await BroadcastToSubscribersAsync(data, logger);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                logger.LogInformation("Polling cancelled for device {DeviceId}", DeviceId);
                break;
            }
            catch (Exception ex)
            {
                consecutiveErrors++;
                logger.LogError(ex, "Error fetching PowerFlow for device {DeviceId} (consecutive errors: {ErrorCount})",
                    DeviceId, consecutiveErrors);

                // After DeviceOfflineThreshold (3) consecutive errors, send zero data to indicate device is offline
                // This causes the frontend to show gray (no flow) state instead of stale cached data
                if (consecutiveErrors == DeviceOfflineThreshold && !IsDeviceOffline)
                {
                    logger.LogWarning(
                        "Device {DeviceId} appears offline after {ErrorCount} consecutive errors - sending zero PowerFlow data",
                        DeviceId, consecutiveErrors);

                    IsDeviceOffline = true;

                    // Create zero PowerFlow data to indicate device is offline
                    var zeroData = new PowerFlowDto
                    {
                        DeviceId = DeviceId,
                        TimestampUtc = DateTimeOffset.UtcNow,
                        SolarPowerKw = 0,
                        GridPowerKw = 0,
                        LoadPowerKw = 0,
                        BatteryPowerKw = 0,
                        GeneratorPowerKw = 0
                    };

                    LastData = zeroData;
                    LastUpdated = DateTime.UtcNow;

                    // Broadcast zero data to all subscribers
                    await BroadcastToSubscribersAsync(zeroData, logger);
                }

                // If too many consecutive errors, add a longer delay to prevent hammering
                if (consecutiveErrors >= maxConsecutiveErrors)
                {
                    logger.LogWarning(
                        "Device {DeviceId} has {ErrorCount} consecutive errors, adding extended delay",
                        DeviceId, consecutiveErrors);

                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
                // Continue polling despite errors
            }

            try
            {
                await Task.Delay(interval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        logger.LogInformation("Polling loop ended for device {DeviceId}", DeviceId);
    }

    private async Task BroadcastToSubscribersAsync(PowerFlowDto data, ILogger logger)
    {
        var deadSubscriptions = new List<string>();

        foreach (var (subscriptionId, subscription) in _subscribers)
        {
            try
            {
                if (!subscription.IsActive)
                {
                    deadSubscriptions.Add(subscriptionId);
                    continue;
                }

                await subscription.SendDataAsync(data);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Failed to send data to subscription {SubscriptionId}, marking as dead",
                    subscriptionId);
                deadSubscriptions.Add(subscriptionId);
            }
        }

        // Clean up dead subscriptions
        foreach (var subscriptionId in deadSubscriptions)
        {
            _subscribers.TryRemove(subscriptionId, out _);
        }

        if (deadSubscriptions.Count > 0)
        {
            logger.LogInformation(
                "Removed {Count} dead subscriptions from device {DeviceId}",
                deadSubscriptions.Count, DeviceId);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _pollingCts?.Cancel();
        _pollingCts?.Dispose();

        // Complete all subscriber channels
        foreach (var subscription in _subscribers.Values)
        {
            subscription.Complete();
        }

        _subscribers.Clear();
    }
}

/// <summary>
/// Represents a single subscription to a device stream.
/// Each subscriber gets their own channel for receiving data.
/// </summary>
internal class DeviceStreamSubscription : IDeviceStreamSubscription
{
    public Guid DeviceId { get; }
    public string SubscriptionId { get; }
    public DateTime SubscribedAt { get; }
    public bool IsActive { get; private set; } = true;

    private readonly Channel<object> _channel;
    private readonly Func<Task> _unsubscribeAction;

    public ChannelReader<object> DataChannel => _channel.Reader;

    public DeviceStreamSubscription(
        Guid deviceId,
        string subscriptionId,
        Func<Task> unsubscribeAction)
    {
        DeviceId = deviceId;
        SubscriptionId = subscriptionId;
        SubscribedAt = DateTime.UtcNow;
        _unsubscribeAction = unsubscribeAction;

        // Bounded channel to prevent memory issues if consumer is slow
        _channel = Channel.CreateBounded<object>(new BoundedChannelOptions(10)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });
    }

    public async ValueTask SendDataAsync(object data)
    {
        if (!IsActive) return;

        try
        {
            await _channel.Writer.WriteAsync(data);
        }
        catch (ChannelClosedException)
        {
            // Channel already closed, ignore
        }
    }

    public void MarkInactive()
    {
        IsActive = false;
    }

    public void Complete()
    {
        IsActive = false;
        _channel.Writer.TryComplete();
    }

    public async ValueTask DisposeAsync()
    {
        if (!IsActive) return;

        IsActive = false;
        _channel.Writer.TryComplete();
        await _unsubscribeAction();
    }
}
