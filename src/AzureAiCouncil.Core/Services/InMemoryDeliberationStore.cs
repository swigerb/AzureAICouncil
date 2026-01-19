using System.Collections.Concurrent;
using AzureAiCouncil.Core.Models;

namespace AzureAiCouncil.Core.Services;

public sealed class InMemoryDeliberationStore : IDeliberationStore
{
    private readonly ConcurrentDictionary<string, Deliberation> _items = new(StringComparer.Ordinal);

    public Deliberation Create(Motion motion)
    {
        var deliberation = new Deliberation
        {
            DeliberationId = Guid.NewGuid().ToString("n"),
            Motion = motion,
            Status = DeliberationStatus.AwaitingCouncil,
        };

        if (!_items.TryAdd(deliberation.DeliberationId, deliberation))
        {
            throw new InvalidOperationException("Failed to create deliberation.");
        }

        return deliberation;
    }

    public bool TryGet(string deliberationId, out Deliberation deliberation) =>
        _items.TryGetValue(deliberationId, out deliberation!);

    public void UpdateStatus(string deliberationId, DeliberationStatus status)
    {
        if (!_items.TryGetValue(deliberationId, out var deliberation))
        {
            throw new KeyNotFoundException($"Deliberation '{deliberationId}' not found.");
        }

        deliberation.Status = status;

        if (status is DeliberationStatus.ResolutionIssued or DeliberationStatus.Failed)
        {
            deliberation.CompletedAt ??= DateTimeOffset.UtcNow;
        }
    }

    public void AddStatement(string deliberationId, CouncilorStatement statement)
    {
        if (!_items.TryGetValue(deliberationId, out var deliberation))
        {
            throw new KeyNotFoundException($"Deliberation '{deliberationId}' not found.");
        }

        lock (deliberation.Statements)
        {
            deliberation.Statements.Add(statement);
        }
    }

    public void SetResolution(string deliberationId, CouncilResolution resolution)
    {
        if (!_items.TryGetValue(deliberationId, out var deliberation))
        {
            throw new KeyNotFoundException($"Deliberation '{deliberationId}' not found.");
        }

        deliberation.Resolution = resolution;
        deliberation.CompletedAt ??= DateTimeOffset.UtcNow;
    }

    public IReadOnlyList<Deliberation> List() => _items.Values.ToArray();
}
