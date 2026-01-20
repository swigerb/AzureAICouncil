using System.Text.Json.Serialization;

using AzureAiCouncil.Core.Serialization;

namespace AzureAiCouncil.Core.Models;

public sealed record CouncilResolution
{
    public required string DeliberationId { get; init; }

    public required string Summary { get; init; }

    public required string Recommendation { get; init; }

    public IReadOnlyList<string> Pros { get; init; } = [];

    public IReadOnlyList<string> Cons { get; init; } = [];

    public CouncilResolutionScores Scores { get; init; } = new();

    public IReadOnlyList<string> WhenThisIsAGoodIdea { get; init; } = [];

    public IReadOnlyList<string> WhenToAvoid { get; init; } = [];

    public IReadOnlyList<string> SuggestedNextSteps { get; init; } = [];

    public string MermaidArchitectureSnippet { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record CouncilResolutionScores
{
    [JsonConverter(typeof(TierJsonConverter))]
    public Tier Risk { get; init; } = Tier.Unknown;

    [JsonConverter(typeof(TierJsonConverter))]
    public Tier Cost { get; init; } = Tier.Unknown;

    [JsonConverter(typeof(TierJsonConverter))]
    public Tier Complexity { get; init; } = Tier.Unknown;

    public double Confidence { get; init; }
}
