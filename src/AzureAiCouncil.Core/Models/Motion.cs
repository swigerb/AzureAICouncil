using System.Text.Json.Serialization;
using AzureAiCouncil.Core.Serialization;

namespace AzureAiCouncil.Core.Models;

public sealed record Motion
{
    public required string MotionId { get; init; }

    public required string Question { get; init; }

    public MotionConstraints Constraints { get; init; } = new();

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record MotionConstraints
{
    public string? Budget { get; init; }

    public string? Timeline { get; init; }

    [JsonConverter(typeof(RiskToleranceJsonConverter))]
    public RiskTolerance RiskTolerance { get; init; } = RiskTolerance.Unspecified;

    public IReadOnlyList<string> TechnologyPreferences { get; init; } = Array.Empty<string>();
}
