using System.Collections.Concurrent;
using AzureAiCouncil.Core.Models;
using AzureAiCouncil.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace AzureAiCouncil.Tests;

public sealed class ChairEngineTests
{
    [Fact]
    public async Task RunAsync_emits_statements_in_deterministic_order_even_if_agents_finish_out_of_order()
    {
        var store = new InMemoryDeliberationStore();
        var eventBus = new InMemoryDeliberationEventBus();

        var motion = new Motion
        {
            MotionId = "m1",
            Question = "Should we build Y?",
            Constraints = new MotionConstraints { RiskTolerance = RiskTolerance.Medium },
        };

        var deliberation = store.Create(motion);

        var councilors = new ICouncilorAgent[]
        {
            new FakeCouncilorAgent(CouncilorKind.Vision, delayMs: 80),
            new FakeCouncilorAgent(CouncilorKind.Risk, delayMs: 10),
            new FakeCouncilorAgent(CouncilorKind.Architecture, delayMs: 60),
            new FakeCouncilorAgent(CouncilorKind.Security, delayMs: 20),
            new FakeCouncilorAgent(CouncilorKind.Finance, delayMs: 40),
        };

        var clerk = new FakeClerkAgent();

        var chair = new ChairEngine(councilors, clerk, store, eventBus, NullLogger<ChairEngine>.Instance);

        var captured = new ConcurrentQueue<DeliberationEvent>();

        using var captureCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await using var subscription = eventBus.Subscribe(deliberation.DeliberationId);

        var captureTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var ev in subscription.Reader.ReadAllAsync(captureCts.Token))
                {
                    captured.Enqueue(ev);

                    if (ev.Type == DeliberationEventType.Resolution)
                    {
                        // We have everything needed for assertions.
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Ignore timeout/cancellation.
            }
        }, captureCts.Token);

        await chair.RunAsync(deliberation.DeliberationId, CancellationToken.None);

        // Give the capture loop a moment to see the final event.
        await Task.WhenAny(captureTask, Task.Delay(500, CancellationToken.None));

        Assert.True(store.TryGet(deliberation.DeliberationId, out var after));
        Assert.Equal(DeliberationStatus.ResolutionIssued, after.Status);
        Assert.NotNull(after.Resolution);

        var statementEvents = captured
            .Where(e => e.Type == DeliberationEventType.Statement)
            .Select(e => Assert.IsType<CouncilorStatement>(e.Data))
            .ToArray();

        Assert.Equal(15, statementEvents.Length);

        var expectedOrder = new[]
        {
            CouncilorKind.Vision,
            CouncilorKind.Risk,
            CouncilorKind.Architecture,
            CouncilorKind.Security,
            CouncilorKind.Finance,
        };

        for (var round = 1; round <= 3; round++)
        {
            var roundStatements = statementEvents.Where(s => s.Round == round).ToArray();
            Assert.Equal(5, roundStatements.Length);

            for (var i = 0; i < expectedOrder.Length; i++)
            {
                Assert.Equal(expectedOrder[i], roundStatements[i].Councilor);
            }
        }
    }

    private sealed class FakeCouncilorAgent : ICouncilorAgent
    {
        private readonly int _delayMs;

        public FakeCouncilorAgent(CouncilorKind kind, int delayMs)
        {
            Kind = kind;
            _delayMs = delayMs;
        }

        public CouncilorKind Kind { get; }

        public async Task<CouncilorStatement> ProduceStatementAsync(
            Motion motion,
            int round,
            string goal,
            IReadOnlyList<CouncilorStatement> priorStatements,
            CancellationToken cancellationToken)
        {
            if (_delayMs > 0)
            {
                await Task.Delay(_delayMs, cancellationToken);
            }

            return new CouncilorStatement
            {
                Councilor = Kind,
                Round = round,
                StanceTitle = $"{Kind} {goal}",
                Answer = $"{Kind} answer r{round}",
                Rationale = "because",
                Confidence = 0.7,
                RisksOrConcerns = ["r"],
                Position = new CouncilorPosition { GoNoGo = GoNoGo.Mixed, Priority = Priority.Medium },
                NotesForCouncil = [],
                MermaidHint = null,
                CostTier = Tier.Unknown,
                ComplexityTier = Tier.Unknown,
            };
        }
    }

    private sealed class FakeClerkAgent : IClerkAgent
    {
        public Task<CouncilResolution> ProduceResolutionAsync(
            string deliberationId,
            Motion motion,
            IReadOnlyList<CouncilorStatement> transcript,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new CouncilResolution
            {
                DeliberationId = deliberationId,
                Summary = "s",
                Recommendation = "r",
                Pros = ["p"],
                Cons = ["c"],
                WhenThisIsAGoodIdea = ["w"],
                WhenToAvoid = ["a"],
                SuggestedNextSteps = ["n"],
                Scores = new CouncilResolutionScores { Confidence = 0.5 },
                MermaidArchitectureSnippet = "graph TD;",
            });
        }
    }

}
