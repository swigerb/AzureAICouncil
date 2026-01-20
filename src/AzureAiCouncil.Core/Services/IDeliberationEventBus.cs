using System.Threading.Channels;

namespace AzureAiCouncil.Core.Services;

/// <summary>
/// Publishes and subscribes to deliberation lifecycle events.
/// </summary>
public interface IDeliberationEventBus
{
    /// <summary>
    /// Creates a subscription to receive events for a specific deliberation.
    /// </summary>
    /// <param name="deliberationId">The deliberation ID to subscribe to.</param>
    /// <returns>A disposable subscription with a channel reader for events.</returns>
    DeliberationEventSubscription Subscribe(string deliberationId);

    /// <summary>
    /// Publishes an event to all subscribers of the corresponding deliberation.
    /// </summary>
    /// <param name="ev">The event to publish.</param>
    void Publish(DeliberationEvent ev);
}

/// <summary>
/// Represents an active subscription to deliberation events.
/// </summary>
public sealed class DeliberationEventSubscription : IAsyncDisposable
{
    private readonly Func<ValueTask> _dispose;

    internal DeliberationEventSubscription(
        string deliberationId,
        ChannelReader<DeliberationEvent> reader,
        Func<ValueTask> dispose)
    {
        DeliberationId = deliberationId;
        Reader = reader;
        _dispose = dispose;
    }

    /// <summary>
    /// Gets the deliberation ID this subscription is for.
    /// </summary>
    public string DeliberationId { get; }

    /// <summary>
    /// Gets the channel reader to receive events from.
    /// </summary>
    public ChannelReader<DeliberationEvent> Reader { get; }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _dispose();
}
