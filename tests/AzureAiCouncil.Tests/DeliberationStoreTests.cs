using AzureAiCouncil.Core.Models;
using AzureAiCouncil.Core.Services;

namespace AzureAiCouncil.Tests;

public sealed class DeliberationStoreTests
{
    [Fact]
    public void Create_sets_default_status_and_timestamps()
    {
        var store = new InMemoryDeliberationStore();

        var motion = new Motion
        {
            MotionId = "m1",
            Question = "Should we adopt X?",
            Constraints = new MotionConstraints { RiskTolerance = RiskTolerance.Unspecified },
        };

        var deliberation = store.Create(motion);

        Assert.False(string.IsNullOrWhiteSpace(deliberation.DeliberationId));
        Assert.Equal(motion, deliberation.Motion);
        Assert.Equal(DeliberationStatus.AwaitingCouncil, deliberation.Status);
        Assert.Empty(deliberation.Statements);
        Assert.Null(deliberation.Resolution);
        Assert.Null(deliberation.CompletedAt);
        Assert.True(deliberation.CreatedAt > DateTimeOffset.MinValue);
    }

    [Fact]
    public void UpdateStatus_sets_completedAt_for_terminal_states_only()
    {
        var store = new InMemoryDeliberationStore();

        var deliberation = store.Create(new Motion
        {
            MotionId = "m1",
            Question = "Q",
            Constraints = new MotionConstraints(),
        });

        store.UpdateStatus(deliberation.DeliberationId, DeliberationStatus.Round1);

        Assert.True(store.TryGet(deliberation.DeliberationId, out var afterRound1));
        Assert.Equal(DeliberationStatus.Round1, afterRound1.Status);
        Assert.Null(afterRound1.CompletedAt);

        store.UpdateStatus(deliberation.DeliberationId, DeliberationStatus.Failed);

        Assert.True(store.TryGet(deliberation.DeliberationId, out var afterFail));
        Assert.Equal(DeliberationStatus.Failed, afterFail.Status);
        Assert.NotNull(afterFail.CompletedAt);
    }

    [Fact]
    public void SetResolution_sets_completedAt()
    {
        var store = new InMemoryDeliberationStore();

        var deliberation = store.Create(new Motion
        {
            MotionId = "m1",
            Question = "Q",
            Constraints = new MotionConstraints(),
        });

        var resolution = new CouncilResolution
        {
            DeliberationId = deliberation.DeliberationId,
            Summary = "summary",
            Recommendation = "go",
            Pros = ["p"],
            Cons = ["c"],
            SuggestedNextSteps = ["n"],
            WhenThisIsAGoodIdea = ["w"],
            WhenToAvoid = ["a"],
            Scores = new CouncilResolutionScores { Confidence = 0.5, Risk = Tier.Medium, Cost = Tier.Low, Complexity = Tier.Medium },
            MermaidArchitectureSnippet = "graph TD;",
        };

        store.SetResolution(deliberation.DeliberationId, resolution);

        Assert.True(store.TryGet(deliberation.DeliberationId, out var after));
        Assert.NotNull(after.Resolution);
        Assert.Equal("summary", after.Resolution!.Summary);
        Assert.NotNull(after.CompletedAt);
    }
}
