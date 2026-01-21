using System.Text.Json.Serialization;

using AzureAiCouncil.Core.Serialization;

namespace AzureAiCouncil.Core.Models;

public sealed record CouncilorStatement
{
    public required CouncilorKind Councilor { get; init; }

    public required int Round { get; init; }

    public required string StanceTitle { get; init; }

    public required string Answer { get; init; }

    public required string Rationale { get; init; }

    public IReadOnlyList<string> RisksOrConcerns { get; init; } = [];

    [JsonConverter(typeof(TierJsonConverter))]
    public Tier CostTier { get; init; } = Tier.Unknown;

    [JsonConverter(typeof(TierJsonConverter))]
    public Tier ComplexityTier { get; init; } = Tier.Unknown;

    public double Confidence { get; init; }

    [JsonConverter(typeof(CouncilorPositionJsonConverter))]
    public CouncilorPosition Position { get; init; } = new();

    public IReadOnlyList<string> NotesForCouncil { get; init; } = [];

    public string? MermaidHint { get; init; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record CouncilorPosition
{
    [JsonConverter(typeof(GoNoGoJsonConverter))]
    public GoNoGo GoNoGo { get; init; } = GoNoGo.Mixed;

    [JsonConverter(typeof(PriorityJsonConverter))]
    public Priority Priority { get; init; } = Priority.Medium;
}
