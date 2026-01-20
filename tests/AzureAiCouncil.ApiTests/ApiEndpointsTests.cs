using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace AzureAiCouncil.ApiTests;

public sealed class ApiEndpointsTests : IClassFixture<ApiTestAppFactory>
{
    private readonly ApiTestAppFactory _factory;

    public ApiEndpointsTests(ApiTestAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task OpenApi_is_exposed_in_development()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Health_endpoints_are_exposed_in_development()
    {
        using var client = _factory.CreateClient();

        var health = await client.GetAsync("/health");
        var alive = await client.GetAsync("/alive");

        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        Assert.Equal(HttpStatusCode.OK, alive.StatusCode);
    }

    [Fact]
    public async Task Create_motion_returns_deliberation_id_and_snapshot_is_retrievable()
    {
        using var client = _factory.CreateClient();

        var create = await client.PostAsJsonAsync("/api/motions", new
        {
            question = "Should we adopt Azure Container Apps for a new internal service?",
            constraints = new
            {
                budget = "medium",
                timeline = "4 weeks",
                riskTolerance = "Medium",
                technologyPreferences = new[] { "dotnet", "container" },
            }
        });

        Assert.Equal(HttpStatusCode.OK, create.StatusCode);

        using var createDoc = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var deliberationId = createDoc.RootElement.GetProperty("deliberationId").GetString();
        Assert.False(string.IsNullOrWhiteSpace(deliberationId));

        var snapshot = await client.GetAsync($"/api/deliberations/{deliberationId}");
        Assert.Equal(HttpStatusCode.OK, snapshot.StatusCode);

        using var snapshotDoc = JsonDocument.Parse(await snapshot.Content.ReadAsStringAsync());
        Assert.Equal(deliberationId, snapshotDoc.RootElement.GetProperty("deliberationId").GetString());
        Assert.True(snapshotDoc.RootElement.TryGetProperty("motion", out _));
        Assert.True(snapshotDoc.RootElement.TryGetProperty("status", out _));
    }
}

public sealed class ApiSseStreamingTests : IClassFixture<KestrelApiFixture>
{
    private readonly KestrelApiFixture _fixture;

    public ApiSseStreamingTests(KestrelApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Sse_stream_emits_ready_and_session_started_events()
    {
        using var client = _fixture.CreateClient();

        var create = await client.PostAsJsonAsync("/api/motions", new
        {
            question = "Pick a logging strategy for a .NET API.",
            constraints = new { }
        });

        create.EnsureSuccessStatusCode();

        using var createDoc = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var deliberationId = createDoc.RootElement.GetProperty("deliberationId").GetString();
        Assert.False(string.IsNullOrWhiteSpace(deliberationId));

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var gotReady = false;
        var gotSessionStarted = false;

        await foreach (var ev in SseTestReader.ReadEventsAsync(client, $"/api/deliberations/{deliberationId}/stream", timeoutCts.Token))
        {
            if (ev.Event == "stream.ready")
            {
                gotReady = true;
            }

            if (ev.Event == "session.started")
            {
                gotSessionStarted = true;
            }

            if (gotReady && gotSessionStarted)
            {
                break;
            }
        }

        Assert.True(gotReady);
        Assert.True(gotSessionStarted);
    }
}
