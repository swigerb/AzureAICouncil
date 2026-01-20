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
    public void TryGet_returns_false_for_nonexistent_id()
    {
        var store = new InMemoryDeliberationStore();

        var found = store.TryGet("nonexistent-id", out var deliberation);

        Assert.False(found);
        Assert.Null(deliberation);
    }

    [Fact]
    public void List_returns_empty_when_no_deliberations()
    {
        var store = new InMemoryDeliberationStore();

        var list = store.List();

        Assert.Empty(list);
    }

    [Fact]
    public void List_returns_all_created_deliberations()
    {
        var store = new InMemoryDeliberationStore();

        var d1 = store.Create(new Motion { MotionId = "m1", Question = "Q1", Constraints = new MotionConstraints() });
        var d2 = store.Create(new Motion { MotionId = "m2", Question = "Q2", Constraints = new MotionConstraints() });
        var d3 = store.Create(new Motion { MotionId = "m3", Question = "Q3", Constraints = new MotionConstraints() });

        var list = store.List();

        Assert.Equal(3, list.Count);
        Assert.Contains(list, d => d.DeliberationId == d1.DeliberationId);
        Assert.Contains(list, d => d.DeliberationId == d2.DeliberationId);
        Assert.Contains(list, d => d.DeliberationId == d3.DeliberationId);
    }

    [Fact]
    public void AddStatement_appends_statement_to_deliberation()
    {
        var store = new InMemoryDeliberationStore();
        var deliberation = store.Create(new Motion { MotionId = "m1", Question = "Q", Constraints = new MotionConstraints() });

        var statement = new CouncilorStatement
        {
            Councilor = CouncilorKind.Vision,
            Round = 1,
            StanceTitle = "Title",
            Answer = "Answer",
            Rationale = "Rationale",
            Confidence = 0.8,
            Position = new CouncilorPosition { GoNoGo = GoNoGo.Go, Priority = Priority.High },
        };

        store.AddStatement(deliberation.DeliberationId, statement);

        Assert.True(store.TryGet(deliberation.DeliberationId, out var updated));
        Assert.Single(updated.Statements);
        Assert.Equal(CouncilorKind.Vision, updated.Statements[0].Councilor);
    }

    [Fact]
    public void AddStatement_throws_KeyNotFoundException_for_nonexistent_deliberation()
    {
        var store = new InMemoryDeliberationStore();

        var statement = new CouncilorStatement
        {
            Councilor = CouncilorKind.Risk,
            Round = 1,
            StanceTitle = "T",
            Answer = "A",
            Rationale = "R",
            Confidence = 0.5,
            Position = new CouncilorPosition { GoNoGo = GoNoGo.Mixed, Priority = Priority.Medium },
        };

        Assert.Throws<KeyNotFoundException>(() => store.AddStatement("nonexistent", statement));
    }

    [Fact]
    public void UpdateStatus_throws_KeyNotFoundException_for_nonexistent_deliberation()
    {
        var store = new InMemoryDeliberationStore();

        Assert.Throws<KeyNotFoundException>(() => store.UpdateStatus("nonexistent", DeliberationStatus.Round1));
    }

    [Fact]
    public void SetResolution_throws_KeyNotFoundException_for_nonexistent_deliberation()
    {
        var store = new InMemoryDeliberationStore();

        var resolution = new CouncilResolution
        {
            DeliberationId = "nonexistent",
            Summary = "s",
            Recommendation = "r",
            Pros = [],
            Cons = [],
            SuggestedNextSteps = [],
            WhenThisIsAGoodIdea = [],
            WhenToAvoid = [],
            Scores = new CouncilResolutionScores(),
            MermaidArchitectureSnippet = "",
        };

        Assert.Throws<KeyNotFoundException>(() => store.SetResolution("nonexistent", resolution));
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
