using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using AzureAiCouncil.Core.Telemetry;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AzureAiCouncil.ApiTests;

public sealed class MotionEndToEndStreamingTests : IClassFixture<FakeOpenAiServerFixture>, IAsyncLifetime
{
    private readonly FakeOpenAiServerFixture _fakeOpenAi;

    private WebApplication? _app;

    private Uri _baseAddress = new("http://127.0.0.1:0");

    public MotionEndToEndStreamingTests(FakeOpenAiServerFixture fakeOpenAi)
    {
        _fakeOpenAi = fakeOpenAi;
    }

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Development",
        });

        builder.WebHost.UseKestrel();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AzureOpenAI:Endpoint"] = _fakeOpenAi.BaseAddress.ToString(),
            ["AzureOpenAI:ApiKey"] = "test-key",
            ["AzureOpenAI:Deployment"] = "test-model",
        });

        ApiBootstrap.ConfigureBuilder(builder);

        _app = builder.Build();
        ApiBootstrap.ConfigureApp(_app);

        await _app.StartAsync();

        var addresses = _app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()?.Addresses;

        var address = addresses?.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(address))
        {
            throw new InvalidOperationException("Failed to determine Kestrel listening address for API end-to-end tests.");
        }

        _baseAddress = new Uri(address);
    }

    public async Task DisposeAsync()
    {
        if (_app is null)
        {
            return;
        }

        try
        {
            await _app.StopAsync();
        }
        finally
        {
            await _app.DisposeAsync();
            _app = null;
        }
    }

    private HttpClient CreateClient()
    {
        var client = new HttpClient
        {
            BaseAddress = _baseAddress,
            Timeout = Timeout.InfiniteTimeSpan,
        };

        return client;
    }

    [Fact]
    public async Task Motion_input_runs_all_agents_emits_mermaid_and_produces_agent_telemetry()
    {
        using var client = CreateClient();

        var captured = new ConcurrentBag<Activity>();

        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == CouncilTelemetry.ActivitySourceName,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => captured.Add(activity),
        };

        ActivitySource.AddActivityListener(listener);

        var create = await client.PostAsJsonAsync("/api/motions", new
        {
            question = "Create an Azure-native web architecture and produce a mermaid diagram that follows the Microsoft Cloud Adoption Framework and best practices.",
            constraints = new
            {
                budget = "$2000/mo",
                timeline = "6 weeks",
                technologyPreferences = Array.Empty<string>(),
            }
        });

        create.EnsureSuccessStatusCode();

        using var createDoc = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var deliberationId = createDoc.RootElement.GetProperty("deliberationId").GetString();
        Assert.False(string.IsNullOrWhiteSpace(deliberationId));

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

        var statementKeys = new HashSet<string>(StringComparer.Ordinal);
        string? resolutionMermaid = null;
        var gotSessionStarted = false;

        await foreach (var ev in SseTestReader.ReadEventsAsync(client, $"/api/deliberations/{deliberationId}/stream", timeoutCts.Token))
        {
            if (ev.Event == "session.started")
            {
                gotSessionStarted = true;
                continue;
            }

            if (ev.Event == "error")
            {
                throw new InvalidOperationException($"SSE error event from server: {ev.Data}");
            }

            if (ev.Event == "statement")
            {
                using var doc = JsonDocument.Parse(ev.Data);
                var councilor = doc.RootElement.GetProperty("councilor").GetString();
                var round = doc.RootElement.GetProperty("round").GetInt32();

                Assert.False(string.IsNullOrWhiteSpace(councilor));
                Assert.InRange(round, 1, 3);

                statementKeys.Add($"{round}:{councilor}");
            }

            if (ev.Event == "resolution")
            {
                using var doc = JsonDocument.Parse(ev.Data);
                resolutionMermaid = doc.RootElement.GetProperty("mermaidArchitectureSnippet").GetString();
                break;
            }
        }

        Assert.True(gotSessionStarted);
        Assert.Equal(15, statementKeys.Count);

        foreach (var councilor in new[] { "Vision", "Risk", "Architecture", "Security", "Finance" })
        {
            for (var round = 1; round <= 3; round++)
            {
                Assert.Contains($"{round}:{councilor}", statementKeys);
            }
        }

        Assert.False(string.IsNullOrWhiteSpace(resolutionMermaid));
        Assert.Contains("graph", resolutionMermaid!, StringComparison.OrdinalIgnoreCase);

        // Telemetry assertions: ensure we emitted agent spans.
        // (We assert counts loosely to avoid overfitting to incidental instrumentation.)
        var names = captured.Select(a => a.DisplayName).ToArray();

        Assert.Contains("council.deliberation", names);
        Assert.Contains("council.speaker.normalize", names);
        Assert.Contains("council.clerk", names);

        var councilorSpanCount = names.Count(n => n == "council.councilor");
        Assert.True(councilorSpanCount >= 15, $"Expected >= 15 councilor spans, saw {councilorSpanCount}.");
    }
}
