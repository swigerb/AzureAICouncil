using System.Text.Json;
using AzureAiCouncil.Core.Models;
using AzureAiCouncil.Core.Services;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace AzureAiCouncil.Agents;

public sealed class SpeakerAgent : ISpeakerAgent
{
    private readonly ChatClientAgent _agent;
    private readonly PromptRenderer _prompts;
    private readonly ILogger<SpeakerAgent> _logger;

    public SpeakerAgent(ChatClientAgent agent, PromptRenderer prompts, ILogger<SpeakerAgent> logger)
    {
        _agent = agent;
        _prompts = prompts;
        _logger = logger;
    }

    public async Task<Motion> NormalizeAsync(Motion motion, CancellationToken cancellationToken)
    {
        // Ask the model to normalize the question + constraints, but keep server-generated IDs.
        var prompt = _prompts.RenderFromFile(PromptCatalog.Speaker, new Dictionary<string, string>());

        var input = new
        {
            question = motion.Question,
            constraints = motion.Constraints,
        };

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "You normalize user requests into a Motion object."),
            new(ChatRole.User, prompt + "\n\nInput:\n" + JsonAgentHelpers.ToJson(input)),
        };

        var options = JsonAgentHelpers.CreateCouncilJsonOptions(0.1f);

        var response = await _agent.RunAsync(messages, thread: null, options: new ChatClientAgentRunOptions(options), cancellationToken).ConfigureAwait(false);
        var text = response.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            return motion;
        }

        // Parse just the fields we allow the speaker to influence.
        using var doc = JsonDocument.Parse(text);
        var root = doc.RootElement;

        var question = root.TryGetProperty("question", out var q) ? q.GetString() : motion.Question;
        var constraints = motion.Constraints;

        if (root.TryGetProperty("constraints", out var c) && c.ValueKind == JsonValueKind.Object)
        {
            constraints = new MotionConstraints
            {
                Budget = c.TryGetProperty("budget", out var b) ? b.GetString() : motion.Constraints.Budget,
                Timeline = c.TryGetProperty("timeline", out var t) ? t.GetString() : motion.Constraints.Timeline,
                RiskTolerance = motion.Constraints.RiskTolerance,
                TechnologyPreferences = c.TryGetProperty("technologyPreferences", out var tp) && tp.ValueKind == JsonValueKind.Array
                    ? tp.EnumerateArray().Select(x => x.GetString()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!).ToArray()
                    : motion.Constraints.TechnologyPreferences,
            };
        }

        var normalized = motion with
        {
            Question = string.IsNullOrWhiteSpace(question) ? motion.Question : question!,
            Constraints = constraints,
        };

        _logger.LogInformation("Speaker normalized motion {MotionId}", motion.MotionId);
        return normalized;
    }
}
