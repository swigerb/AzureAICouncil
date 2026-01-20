using System.Diagnostics;
using System.Text.Json;
using AzureAiCouncil.Core.Models;
using AzureAiCouncil.Core.Services;
using AzureAiCouncil.Core.Telemetry;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace AzureAiCouncil.Agents;

public sealed class CouncilorAgent : ICouncilorAgent
{
    private readonly ChatClientAgent _agent;
    private readonly CouncilTools _tools;
    private readonly PromptRenderer _prompts;
    private readonly ILogger<CouncilorAgent> _logger;

    public CouncilorAgent(
        CouncilorKind kind,
        ChatClientAgent agent,
        CouncilTools tools,
        PromptRenderer prompts,
        ILogger<CouncilorAgent> logger)
    {
        Kind = kind;
        _agent = agent;
        _tools = tools;
        _prompts = prompts;
        _logger = logger;
    }

    public CouncilorKind Kind { get; }

    public async Task<CouncilorStatement> ProduceStatementAsync(
        Motion motion,
        int round,
        string goal,
        IReadOnlyList<CouncilorStatement> priorStatements,
        CancellationToken cancellationToken)
    {
        using var activity = CouncilTelemetry.ActivitySource.StartActivity("council.councilor", ActivityKind.Internal);
        activity?.SetTag(CouncilTelemetry.Tags.Round, round);
        activity?.SetTag(CouncilTelemetry.Tags.Councilor, Kind.ToString());
        activity?.SetTag(CouncilTelemetry.Tags.MotionId, motion.MotionId);

        var promptPath = Kind switch
        {
            CouncilorKind.Vision => PromptCatalog.Vision,
            CouncilorKind.Risk => PromptCatalog.Risk,
            CouncilorKind.Architecture => PromptCatalog.Architecture,
            CouncilorKind.Security => PromptCatalog.Security,
            CouncilorKind.Finance => PromptCatalog.Finance,
            _ => throw new InvalidOperationException($"Unknown councilor '{Kind}'."),
        };

        var motionJson = JsonAgentHelpers.ToJson(motion);

        var peerStatements = priorStatements.Count == 0
            ? ""
            : JsonSerializer.Serialize(priorStatements, JsonAgentHelpers.JsonOptions);

        var prompt = _prompts.RenderFromFile(promptPath, new Dictionary<string, string>
        {
            ["Round"] = round.ToString(),
            ["Goal"] = goal,
            ["MotionJson"] = motionJson,
            ["PeerStatements"] = peerStatements,
        });

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "You are a deliberative councilor in Azure AI Council."),
            new(ChatRole.User, prompt),
        };

        var temperature = round == 3 ? 0.2f : 0.7f;
        var options = JsonAgentHelpers.CreateCouncilJsonOptions(temperature, _tools.Tools);

        var json = await GetJsonTextWithRepairAsync(messages, options, expectedCouncilor: Kind, expectedRound: round, cancellationToken);

        var statement = JsonAgentHelpers.ParseJsonOrThrow<CouncilorStatement>(json);

        // Enforce invariants server-side.
        statement = statement with { Councilor = Kind, Round = round };

        _logger.LogInformation("Councilor {Councilor} produced statement for round {Round}", Kind, round);
        return statement;
    }

    private async Task<string> GetJsonTextWithRepairAsync(
        IReadOnlyList<ChatMessage> messages,
        ChatOptions options,
        CouncilorKind expectedCouncilor,
        int expectedRound,
        CancellationToken cancellationToken)
    {
        try
        {
            return await GetJsonTextAsync(messages, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invalid JSON from {Councilor} round {Round}; retrying once", expectedCouncilor, expectedRound);

            var repair = new List<ChatMessage>(messages)
            {
                new(ChatRole.User, "Your last response was invalid or did not match the schema. Output ONLY a single valid JSON object that matches the required schema. No markdown, no prose.")
            };

            return await GetJsonTextAsync(repair, options, cancellationToken);
        }
    }

    private async Task<string> GetJsonTextAsync(IReadOnlyList<ChatMessage> messages, ChatOptions options, CancellationToken cancellationToken)
    {
        var response = await _agent.RunAsync(messages, thread: null, options: new ChatClientAgentRunOptions(options), cancellationToken);
        var text = response.Text;

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Model returned empty response.");
        }

        // Small normalization for accidental code fences.
        text = text.Trim();
        if (text.StartsWith("```", StringComparison.Ordinal))
        {
            text = text.Trim('`');
            var idx = text.IndexOf('{');
            if (idx >= 0)
            {
                text = text.Substring(idx);
            }
        }

        // Validate parse early.
        _ = JsonAgentHelpers.ParseJsonOrThrow<JsonElement>(text);
        return text;
    }
}
