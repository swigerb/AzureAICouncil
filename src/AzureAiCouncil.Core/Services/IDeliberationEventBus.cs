namespace AzureAiCouncil.Core.Services;

public interface IDeliberationEventBus
{
    DeliberationEventSubscription Subscribe(string deliberationId);

    void Publish(DeliberationEvent ev);
}

public sealed class DeliberationEventSubscription : IAsyncDisposable
{
    private readonly Func<ValueTask> _dispose;

    internal DeliberationEventSubscription(string deliberationId, System.Threading.Channels.ChannelReader<DeliberationEvent> reader, Func<ValueTask> dispose)
    {
        DeliberationId = deliberationId;
        Reader = reader;
        _dispose = dispose;
    }

    public string DeliberationId { get; }

    public System.Threading.Channels.ChannelReader<DeliberationEvent> Reader { get; }

    public ValueTask DisposeAsync() => _dispose();
}
