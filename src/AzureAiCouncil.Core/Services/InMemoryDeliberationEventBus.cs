using System.Collections.Concurrent;
using System.Threading.Channels;

namespace AzureAiCouncil.Core.Services;

public sealed class InMemoryDeliberationEventBus : IDeliberationEventBus
{
    private sealed record Subscriber(Guid Id, Channel<DeliberationEvent> Channel);

    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, Subscriber>> _subscribers = new(StringComparer.Ordinal);

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
