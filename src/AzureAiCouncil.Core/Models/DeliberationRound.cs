namespace AzureAiCouncil.Core.Models;

public sealed record DeliberationRound
{
    public required int Round { get; init; }

    public required string Purpose { get; init; }
}
