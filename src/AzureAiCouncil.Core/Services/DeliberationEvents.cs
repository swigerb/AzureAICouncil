using AzureAiCouncil.Core.Models;

namespace AzureAiCouncil.Core.Services;

public enum DeliberationEventType
{
    SessionStarted,
    RoundStarted,
    CouncilorStatus,
    Statement,
    RoundCompleted,
    Resolution,
    Error,
}

public sealed record DeliberationEvent
{
    public required long EventId { get; init; }

    public required string DeliberationId { get; init; }

    public required DeliberationEventType Type { get; init; }

    public required DateTimeOffset Timestamp { get; init; }

    public object? Data { get; init; }
}

public sealed record CouncilorStatusUpdate
{
    public required int Round { get; init; }

    public required CouncilorKind Councilor { get; init; }

    public required string State { get; init; }
}

public sealed record RoundStartedUpdate
{
    public required int Round { get; init; }

    public required string Goal { get; init; }
}

public sealed record ErrorUpdate
{
    public required string Where { get; init; }

    public required string Message { get; init; }

    public bool Retryable { get; init; }
}
