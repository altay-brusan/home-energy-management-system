using System.Threading.Channels;

namespace Hems_Backend.Application.Common.Interfaces;

/// <summary>
/// Configuration options for shared device streams
/// </summary>
public class SharedStreamOptions
{
    /// <summary>
    /// Default polling interval in seconds (used if subscriber doesn't specify)
    /// </summary>
    public int DefaultPollingIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// Minimum allowed polling interval in seconds
    /// </summary>
    public int MinPollingIntervalSeconds { get; set; } = 2;

    /// <summary>
    /// Maximum allowed polling interval in seconds
    /// </summary>
    public int MaxPollingIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Grace period in seconds before cleaning up stream after last subscriber leaves.
    /// This helps avoid recreating streams for rapid reconnections.
    /// </summary>
    public int CleanupDelaySeconds { get; set; } = 5;

    /// <summary>
    /// Maximum age of cached data in seconds before it's considered stale.
    /// New subscribers will wait for fresh data if cache is stale.
    /// </summary>
    public int StaleDataThresholdSeconds { get; set; } = 30;
}

/// <summary>
/// Statistics about the shared stream manager
/// </summary>
public class SharedStreamStatistics
{
    /// <summary>
    /// Number of active device streams (devices being polled)
    /// </summary>
    public int ActiveStreamCount { get; init; }

    /// <summary>
    /// Total number of subscribers across all streams
    /// </summary>
    public int TotalSubscriberCount { get; init; }

    /// <summary>
    /// Breakdown of subscribers per device
    /// </summary>
    public Dictionary<Guid, int> SubscribersPerDevice { get; init; } = new();
}

/// <summary>
/// Represents an active subscription to a device's data stream.
/// Dispose this to unsubscribe from the stream.
/// </summary>
public interface IDeviceStreamSubscription : IAsyncDisposable
{
    /// <summary>
    /// The device ID this subscription is for
    /// </summary>
    Guid DeviceId { get; }

    /// <summary>
    /// Unique identifier for this subscription
    /// </summary>
    string SubscriptionId { get; }

    /// <summary>
    /// Channel reader for receiving PowerFlow data.
    /// Read from this to get streaming updates.
    /// </summary>
    ChannelReader<object> DataChannel { get; }

    /// <summary>
    /// Whether this subscription is still active
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Timestamp when this subscription was created
    /// </summary>
    DateTime SubscribedAt { get; }
}

/// <summary>
/// Manages shared device PowerFlow streams with reference counting.
/// Ensures only one polling loop per device regardless of subscriber count.
/// Automatically cleans up when last subscriber disconnects.
/// </summary>
public interface ISharedDevicePowerFlowStreamManager
{
    /// <summary>
    /// Subscribe to a device's data stream.
    /// If no stream exists, creates one and starts polling.
    /// If stream exists, adds subscriber to existing stream.
    /// </summary>
    /// <param name="deviceId">The device to subscribe to</param>
    /// <param name="intervalSeconds">Desired polling interval (may use existing stream's interval)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Subscription handle - dispose to unsubscribe</returns>
    Task<IDeviceStreamSubscription> SubscribeAsync(
        Guid deviceId,
        int intervalSeconds = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unsubscribe from a device's data stream.
    /// If this is the last subscriber, stops polling and removes stream after cleanup delay.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID to remove</param>
    /// <returns>True if subscription was found and removed</returns>
    Task<bool> UnsubscribeAsync(string subscriptionId);

    /// <summary>
    /// Get statistics about active streams and subscribers
    /// </summary>
    SharedStreamStatistics GetStatistics();

    /// <summary>
    /// Get the number of active device streams
    /// </summary>
    int GetActiveStreamCount();

    /// <summary>
    /// Get the total number of subscribers across all streams
    /// </summary>
    int GetTotalSubscriberCount();

    /// <summary>
    /// Get the number of subscribers for a specific device
    /// </summary>
    int GetSubscriberCount(Guid deviceId);

    /// <summary>
    /// Check if a device has any active subscribers
    /// </summary>
    bool HasActiveSubscribers(Guid deviceId);
}
