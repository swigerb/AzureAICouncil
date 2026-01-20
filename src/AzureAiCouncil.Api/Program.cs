using System.Text.Json;
using System.Text.Json.Serialization;
using AzureAiCouncil.Agents;
using AzureAiCouncil.Api.Streaming;
using AzureAiCouncil.Core.Models;
using AzureAiCouncil.Core.Services;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);
ApiBootstrap.ConfigureBuilder(builder);

var app = builder.Build();
ApiBootstrap.ConfigureApp(app);

app.Run();

public partial class Program;

public static class ApiBootstrap
{
    public static void ConfigureBuilder(WebApplicationBuilder builder)
    {
        builder.AddServiceDefaults();

        builder.Services.ConfigureHttpJsonOptions(o =>
        {
            o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        builder.Services.AddOpenApi();

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("spa", policy =>
            {
                policy.WithOrigins("http://localhost:5173")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        builder.Services.AddSingleton<IDeliberationStore, InMemoryDeliberationStore>();
        builder.Services.AddSingleton<IDeliberationEventBus, InMemoryDeliberationEventBus>();

        builder.Services.AddAzureAiCouncilAgents(builder.Configuration);
        builder.Services.AddSingleton<ChairEngine>();
    }

    public static void ConfigureApp(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseCors("spa");

        app.MapDefaultEndpoints();

        app.MapPost("/api/motions", async (
            CreateMotionRequest request,
            ISpeakerAgent speaker,
            IDeliberationStore store,
            ChairEngine chair,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            var motion = new Motion
            {
                MotionId = Guid.NewGuid().ToString("n"),
                Question = request.Question,
                Constraints = new MotionConstraints
                {
                    Budget = request.Constraints?.Budget,
                    Timeline = request.Constraints?.Timeline,
                    RiskTolerance = request.Constraints?.RiskTolerance ?? RiskTolerance.Unspecified,
                    TechnologyPreferences = request.Constraints?.TechnologyPreferences ?? Array.Empty<string>(),
                },
            };

            var normalized = await speaker.NormalizeAsync(motion, cancellationToken);
            var deliberation = store.Create(normalized);

            var logger = loggerFactory.CreateLogger("DeliberationRunner");

            // Run in background so the POST can return quickly; the UI watches via SSE.
            _ = Task.Run(async () =>
            {
                try
                {
                    await chair.RunAsync(deliberation.DeliberationId, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Background deliberation failed for {DeliberationId}", deliberation.DeliberationId);
                }
            });

            return Results.Ok(new CreateMotionResponse
            {
                DeliberationId = deliberation.DeliberationId,
                Motion = normalized,
                Resolution = deliberation.Resolution,
            });
        });

        app.MapGet("/api/deliberations/{id}", (string id, IDeliberationStore store) =>
        {
            return store.TryGet(id, out var deliberation)
                ? Results.Ok(new GetDeliberationResponse
                {
                    DeliberationId = deliberation.DeliberationId,
                    Motion = deliberation.Motion,
                    Status = deliberation.Status,
                    Rounds = deliberation.Statements
                        .GroupBy(s => s.Round)
                        .OrderBy(g => g.Key)
                        .Select(g => new DeliberationRoundView
                        {
                            Round = g.Key,
                            Statements = g.ToArray(),
                        })
                        .ToArray(),
                    Resolution = deliberation.Resolution,
                })
                : Results.NotFound();
        });

        app.MapGet("/api/deliberations/{id}/stream", async (string id, HttpContext context, IDeliberationEventBus bus) =>
        {
            context.Response.Headers.CacheControl = "no-cache";
            context.Response.Headers.Connection = "keep-alive";
            context.Response.ContentType = "text/event-stream";

            await using var subscription = bus.Subscribe(id);

            await SseWriter.WriteEventAsync(context, "stream.ready", "{}", eventId: null, context.RequestAborted);

            await foreach (var ev in subscription.Reader.ReadAllAsync(context.RequestAborted))
            {
                var eventName = ev.Type switch
                {
                    DeliberationEventType.SessionStarted => "session.started",
                    DeliberationEventType.RoundStarted => "round.started",
                    DeliberationEventType.CouncilorStatus => "councilor.status",
                    DeliberationEventType.Statement => "statement",
                    DeliberationEventType.RoundCompleted => "round.completed",
                    DeliberationEventType.Resolution => "resolution",
                    DeliberationEventType.Error => "error",
                    _ => "event",
                };

                var data = JsonSerializer.Serialize(ev.Data, JsonAgentHelpers.JsonOptions);
                await SseWriter.WriteEventAsync(context, eventName, data, ev.EventId.ToString(), context.RequestAborted);
            }
        });
    }
}

sealed record CreateMotionRequest
{
    public required string Question { get; init; }
    public CreateMotionConstraints? Constraints { get; init; }
}

sealed record CreateMotionConstraints
{
    public string? Budget { get; init; }
    public string? Timeline { get; init; }
    public RiskTolerance? RiskTolerance { get; init; }
    public IReadOnlyList<string>? TechnologyPreferences { get; init; }
}

sealed record CreateMotionResponse
{
    public required string DeliberationId { get; init; }
    public required Motion Motion { get; init; }
    public CouncilResolution? Resolution { get; init; }
}

sealed record GetDeliberationResponse
{
    public required string DeliberationId { get; init; }
    public required Motion Motion { get; init; }
    public required DeliberationStatus Status { get; init; }
    public required IReadOnlyList<DeliberationRoundView> Rounds { get; init; }
    public CouncilResolution? Resolution { get; init; }
}

sealed record DeliberationRoundView
{
    public required int Round { get; init; }
    public required IReadOnlyList<CouncilorStatement> Statements { get; init; }
}
