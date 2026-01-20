using System.Reflection;
using AzureAiCouncil.Agents;
using AzureAiCouncil.Core.Models;
using AzureAiCouncil.Core.Services;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AzureAiCouncil.ApiTests;

public sealed class AgentsEndToEndWithAgentFrameworkTests : IClassFixture<FakeOpenAiServerFixture>
{
    private readonly FakeOpenAiServerFixture _fakeOpenAi;

    public AgentsEndToEndWithAgentFrameworkTests(FakeOpenAiServerFixture fakeOpenAi)
    {
        _fakeOpenAi = fakeOpenAi;
    }

    [Fact]
    public async Task Agents_run_end_to_end_via_agent_framework_and_exchange_transcript()
    {
        var services = new ServiceCollection();

        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureOpenAI:Endpoint"] = _fakeOpenAi.BaseAddress.ToString(),
                ["AzureOpenAI:ApiKey"] = "test-key",
                ["AzureOpenAI:Deployment"] = "test-model",
            })
            .Build();

        services.AddSingleton<IConfiguration>(cfg);
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));

        services.AddSingleton<IDeliberationStore, InMemoryDeliberationStore>();
        services.AddSingleton<IDeliberationEventBus, InMemoryDeliberationEventBus>();

        services.AddAzureAiCouncilAgents(cfg);
        services.AddSingleton<ChairEngine>();

        await using var provider = services.BuildServiceProvider();

        var speaker = provider.GetRequiredService<ISpeakerAgent>();
        var clerk = provider.GetRequiredService<IClerkAgent>();
        var councilors = provider.GetServices<ICouncilorAgent>().ToArray();

        // Validate that we're not in mock mode and that the role wrappers are backed by ChatClientAgent.
        Assert.IsType<SpeakerAgent>(speaker);
        Assert.IsType<ClerkAgent>(clerk);
        Assert.Equal(5, councilors.Length);
        Assert.All(councilors, c => Assert.IsType<CouncilorAgent>(c));

        AssertChatClientAgentFieldIsPresentAndNonNull(speaker);
        AssertChatClientAgentFieldIsPresentAndNonNull(clerk);
        foreach (var councilor in councilors)
        {
            AssertChatClientAgentFieldIsPresentAndNonNull(councilor);
        }

        var motion = new Motion
        {
            MotionId = "m1",
            Question = "Should we adopt Azure Container Apps for a new internal service?",
            Constraints = new MotionConstraints
            {
                Budget = "medium",
                Timeline = "4 weeks",
                RiskTolerance = RiskTolerance.Medium,
                TechnologyPreferences = new[] { "dotnet", "containers" },
            },
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var normalized = await speaker.NormalizeAsync(motion, cts.Token);
        Assert.False(string.IsNullOrWhiteSpace(normalized.Question));

        var store = provider.GetRequiredService<IDeliberationStore>();
        var deliberation = store.Create(normalized);

        var chair = provider.GetRequiredService<ChairEngine>();
        await chair.RunAsync(deliberation.DeliberationId, cts.Token);

        Assert.True(store.TryGet(deliberation.DeliberationId, out var finished));
        Assert.NotNull(finished.Resolution);

        // 5 councilors x 3 rounds
        Assert.Equal(15, finished.Statements.Count);

        // Basic invariant checks.
        Assert.All(finished.Statements, s => Assert.False(string.IsNullOrWhiteSpace(s.Answer)));
        Assert.All(finished.Statements, s => Assert.InRange(s.Round, 1, 3));

        // Confirm we exercised the Agent Framework-backed HTTP path.
        Assert.True(_fakeOpenAi.RequestCount >= 17, $"Expected at least 17 model calls, saw {_fakeOpenAi.RequestCount}.");

        // Confirm the clerk call included transcript data (agents exchanging context).
        Assert.Contains(_fakeOpenAi.Bodies, b => b.Contains("transcript", StringComparison.OrdinalIgnoreCase));
    }

    private static void AssertChatClientAgentFieldIsPresentAndNonNull(object instance)
    {
        var field = instance.GetType().GetField("_agent", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);

        var value = field!.GetValue(instance);
        Assert.NotNull(value);
        Assert.IsType<ChatClientAgent>(value);
    }
}
