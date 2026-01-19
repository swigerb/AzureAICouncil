using System.Threading.Channels;
using AzureAiCouncil.Core.Services;

namespace AzureAiCouncil.Tests;

public sealed class DeliberationEventBusTests
{
    [Fact]
    public async Task Publish_delivers_to_all_subscribers_for_id()
    {
        var bus = new InMemoryDeliberationEventBus();
        var deliberationId = "d1";

        await using var sub1 = bus.Subscribe(deliberationId);
        await using var sub2 = bus.Subscribe(deliberationId);

        var ev = new DeliberationEvent
        {
            EventId = 1,
            DeliberationId = deliberationId,
            Type = DeliberationEventType.Statement,
            Timestamp = DateTimeOffset.UtcNow,
            Data = new { hello = "world" },
        };

        bus.Publish(ev);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        var r1 = await ReadOneAsync(sub1.Reader, cts.Token);
        var r2 = await ReadOneAsync(sub2.Reader, cts.Token);

        Assert.Equal(DeliberationEventType.Statement, r1.Type);
        Assert.Equal(DeliberationEventType.Statement, r2.Type);
        Assert.Equal(deliberationId, r1.DeliberationId);
        Assert.Equal(deliberationId, r2.DeliberationId);
    }

    [Fact]
    public async Task Publish_does_nothing_when_no_subscribers()
    {
        var bus = new InMemoryDeliberationEventBus();

        bus.Publish(new DeliberationEvent
        {
            EventId = 1,
            DeliberationId = "nope",
            Type = DeliberationEventType.Statement,
            Timestamp = DateTimeOffset.UtcNow,
            Data = null,
        });

        // No exception is the contract here.
        await Task.CompletedTask;
    }

    private static async Task<DeliberationEvent> ReadOneAsync(ChannelReader<DeliberationEvent> reader, CancellationToken cancellationToken)
    {
        return await reader.ReadAsync(cancellationToken);
    }
}
