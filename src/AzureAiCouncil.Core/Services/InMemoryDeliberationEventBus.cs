using System.Collections.Concurrent;
using System.Threading.Channels;

namespace AzureAiCouncil.Core.Services;

public sealed class InMemoryDeliberationEventBus : IDeliberationEventBus
{
    private sealed record Subscriber(Guid Id, Channel<DeliberationEvent> Channel);

    private const int MaxHistoryEventsPerDeliberation = 256;

    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, Subscriber>> _subscribers = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, ConcurrentQueue<DeliberationEvent>> _history = new(StringComparer.Ordinal);

    public DeliberationEventSubscription Subscribe(string deliberationId)
    {
        var channel = Channel.CreateUnbounded<DeliberationEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false,
        });

        var subscriber = new Subscriber(Guid.NewGuid(), channel);

        var map = _subscribers.GetOrAdd(deliberationId, _ => new ConcurrentDictionary<Guid, Subscriber>());
        map.TryAdd(subscriber.Id, subscriber);

        // Best-effort replay of recent events so late subscribers can catch up.
        if (_history.TryGetValue(deliberationId, out var history))
        {
            foreach (var ev in history.ToArray())
            {
                channel.Writer.TryWrite(ev);
            }
        }

        return new DeliberationEventSubscription(
            deliberationId,
            channel.Reader,
            dispose: () =>
            {
                if (_subscribers.TryGetValue(deliberationId, out var existing))
                {
                    existing.TryRemove(subscriber.Id, out _);
                }

                channel.Writer.TryComplete();
                return ValueTask.CompletedTask;
            });
    }

    public void Publish(DeliberationEvent ev)
    {
        var history = _history.GetOrAdd(ev.DeliberationId, _ => new ConcurrentQueue<DeliberationEvent>());
        history.Enqueue(ev);

        while (history.Count > MaxHistoryEventsPerDeliberation && history.TryDequeue(out _))
        {
        }

        if (!_subscribers.TryGetValue(ev.DeliberationId, out var map))
        {
            return;
        }

        foreach (var kvp in map)
        {
            kvp.Value.Channel.Writer.TryWrite(ev);
        }
    }
}
