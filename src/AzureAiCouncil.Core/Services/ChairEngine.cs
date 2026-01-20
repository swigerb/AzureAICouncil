using System.Collections.Concurrent;
using System.Diagnostics;
using AzureAiCouncil.Core.Models;
using AzureAiCouncil.Core.Telemetry;
using Microsoft.Extensions.Logging;

namespace AzureAiCouncil.Core.Services;

public sealed class ChairEngine
{
    private static readonly CouncilorKind[] CouncilorOrder =
    [
        CouncilorKind.Vision,
        CouncilorKind.Risk,
        CouncilorKind.Architecture,
        CouncilorKind.Security,
        CouncilorKind.Finance,
    ];

    private long _eventId;

    private readonly IReadOnlyDictionary<CouncilorKind, ICouncilorAgent> _councilors;
    private readonly IClerkAgent _clerk;
    private readonly IDeliberationStore _store;
    private readonly IDeliberationEventBus _events;
    private readonly ILogger<ChairEngine> _logger;

    public ChairEngine(
        IEnumerable<ICouncilorAgent> councilors,
        IClerkAgent clerk,
        IDeliberationStore store,
        IDeliberationEventBus events,
        ILogger<ChairEngine> logger)
    {
        _councilors = councilors.ToDictionary(c => c.Kind);
        _clerk = clerk;
        _store = store;
        _events = events;
        _logger = logger;
    }

    public async Task RunAsync(string deliberationId, CancellationToken cancellationToken)
    {
        if (!_store.TryGet(deliberationId, out var deliberation))
        {
            throw new KeyNotFoundException($"Deliberation '{deliberationId}' not found.");
        }

        using var activity = CouncilTelemetry.ActivitySource.StartActivity("council.deliberation", ActivityKind.Internal);
        activity?.SetTag(CouncilTelemetry.Tags.DeliberationId, deliberationId);
        activity?.SetTag(CouncilTelemetry.Tags.MotionId, deliberation.Motion.MotionId);

        Publish(deliberationId, DeliberationEventType.SessionStarted, new
        {
            deliberationId,
            motionId = deliberation.Motion.MotionId,
            councilors = CouncilorOrder.Select(c => new { councilor = c.ToString(), order = Array.IndexOf(CouncilorOrder, c) }),
            roundsPlanned = 3,
        });

        try
        {
            await RunRoundAsync(deliberationId, deliberation.Motion, 1, "initial", cancellationToken).ConfigureAwait(false);
            await RunRoundAsync(deliberationId, deliberation.Motion, 2, "critique_refine", cancellationToken).ConfigureAwait(false);
            await RunRoundAsync(deliberationId, deliberation.Motion, 3, "final_vote", cancellationToken).ConfigureAwait(false);

            var transcript = deliberation.Statements.ToArray();

            Publish(deliberationId, DeliberationEventType.CouncilorStatus, new CouncilorStatusUpdate
            {
                Round = 0,
                Councilor = CouncilorKind.Vision,
                State = "clerk_running",
            });

            var resolution = await _clerk.ProduceResolutionAsync(deliberationId, deliberation.Motion, transcript, cancellationToken).ConfigureAwait(false);

            _store.SetResolution(deliberationId, resolution);
            _store.UpdateStatus(deliberationId, DeliberationStatus.ResolutionIssued);

            Publish(deliberationId, DeliberationEventType.Resolution, resolution);

            _logger.LogInformation("Resolution issued for {DeliberationId}", deliberationId);
        }
        catch (Exception ex)
        {
            _store.UpdateStatus(deliberationId, DeliberationStatus.Failed);
            Publish(deliberationId, DeliberationEventType.Error, new ErrorUpdate
            {
                Where = "chair",
                Message = ex.Message,
                Retryable = false,
            });
            _logger.LogError(ex, "Deliberation failed for {DeliberationId}", deliberationId);
            throw;
        }
    }

    private async Task RunRoundAsync(string deliberationId, Motion motion, int round, string goal, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting round {Round} for {DeliberationId}", round, deliberationId);

        _store.UpdateStatus(deliberationId, round switch
        {
            1 => DeliberationStatus.Round1,
            2 => DeliberationStatus.Round2,
            3 => DeliberationStatus.Round3,
            _ => DeliberationStatus.Round1,
        });

        Publish(deliberationId, DeliberationEventType.RoundStarted, new RoundStartedUpdate { Round = round, Goal = goal });

        if (!_store.TryGet(deliberationId, out var deliberation))
        {
            throw new KeyNotFoundException($"Deliberation '{deliberationId}' not found.");
        }

        IReadOnlyList<CouncilorStatement> priorStatements;
        lock (deliberation.Statements)
        {
            priorStatements = deliberation.Statements.Where(s => s.Round < round).ToArray();
        }

        var tasks = new ConcurrentDictionary<CouncilorKind, Task<CouncilorStatement>>();
        foreach (var councilorKind in CouncilorOrder)
        {
            if (!_councilors.TryGetValue(councilorKind, out var agent))
            {
                throw new InvalidOperationException($"Missing agent for councilor '{councilorKind}'.");
            }

            Publish(deliberationId, DeliberationEventType.CouncilorStatus, new CouncilorStatusUpdate
            {
                Round = round,
                Councilor = councilorKind,
                State = "running",
            });

            tasks[councilorKind] = agent.ProduceStatementAsync(motion, round, goal, priorStatements, cancellationToken);
        }

        var emitted = new List<CouncilorStatement>();

        foreach (var councilorKind in CouncilorOrder)
        {
            var statement = await tasks[councilorKind].ConfigureAwait(false);

            Publish(deliberationId, DeliberationEventType.CouncilorStatus, new CouncilorStatusUpdate
            {
                Round = round,
                Councilor = councilorKind,
                State = "done_waiting_to_emit",
            });

            _store.AddStatement(deliberationId, statement);
            emitted.Add(statement);

            Publish(deliberationId, DeliberationEventType.Statement, statement);
        }

        Publish(deliberationId, DeliberationEventType.RoundCompleted, new
        {
            round,
            goal,
            outputs = emitted,
        });

        _logger.LogInformation("Completed round {Round} for {DeliberationId}", round, deliberationId);
    }

    private void Publish(string deliberationId, DeliberationEventType type, object? data)
    {
        var id = Interlocked.Increment(ref _eventId);
        _events.Publish(new DeliberationEvent
        {
            EventId = id,
            DeliberationId = deliberationId,
            Type = type,
            Timestamp = DateTimeOffset.UtcNow,
            Data = data,
        });
    }
}
