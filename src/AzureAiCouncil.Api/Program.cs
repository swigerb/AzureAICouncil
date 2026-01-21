using System.Text.Json;
using System.Text.Json.Serialization;
using AzureAiCouncil.Agents;
using AzureAiCouncil.Api.Streaming;
using AzureAiCouncil.Core.Models;
using AzureAiCouncil.Core.Services;
using AzureAiCouncil.Core.Telemetry;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

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
                // In development the SPA may run on varying ports (Vite, Aspire, etc.).
                // Allow loopback origins broadly to keep SSE and fetch working.
                if (builder.Environment.IsDevelopment())
                {
                    policy
                        .SetIsOriginAllowed(origin =>
                        {
                            if (string.IsNullOrWhiteSpace(origin)) return false;
                            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)) return false;

                            return uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                                   || uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
                                   || uri.Host.Equals("[::1]", StringComparison.OrdinalIgnoreCase);
                        })
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                }
                else
                {
                    policy.WithOrigins("http://localhost:5173")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                }
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

            app.MapGet("/api/debug/config", (
                IHostEnvironment env,
                IConfiguration config,
                IOptions<AzureOpenAiOptions> aoaiOptions) =>
            {
                var aoai = aoaiOptions.Value;
                var otlp = config["OTEL_EXPORTER_OTLP_ENDPOINT"];
                if (string.IsNullOrWhiteSpace(otlp))
                {
                    otlp = config["ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL"];
                }

                string? endpointHost = null;
                if (!string.IsNullOrWhiteSpace(aoai.Endpoint) && Uri.TryCreate(aoai.Endpoint, UriKind.Absolute, out var endpointUri))
                {
                    endpointHost = endpointUri.Host;
                }

                return Results.Ok(new DebugConfigResponse
                {
                    Environment = env.EnvironmentName,
                    ApplicationName = env.ApplicationName,
                    AssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
                    AspNetCoreUrls = config["ASPNETCORE_URLS"],
                    OtlpEndpoint = otlp,
                    Agents = new DebugAgentConfig
                    {
                        AzureOpenAiConfigured = aoai.IsConfigured,
                        AzureOpenAiEndpointHost = endpointHost,
                        AzureOpenAiDeployment = string.IsNullOrWhiteSpace(aoai.Deployment) ? null : aoai.Deployment,
                    },
                });
            });
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
            using var activity = CouncilTelemetry.ActivitySource.StartActivity("api.motion.create", ActivityKind.Server);

            var motion = new Motion
            {
                MotionId = Guid.NewGuid().ToString("n"),
                Question = request.Question,
                Constraints = new MotionConstraints
                {
                    Budget = request.Constraints?.Budget,
                    Timeline = request.Constraints?.Timeline,
                    RiskTolerance = request.Constraints?.RiskTolerance ?? RiskTolerance.Unspecified,
                    TechnologyPreferences = request.Constraints?.TechnologyPreferences ?? [],
                },
            };

            var normalized = await speaker.NormalizeAsync(motion, cancellationToken);
            var deliberation = store.Create(normalized);

            activity?.SetTag(CouncilTelemetry.Tags.DeliberationId, deliberation.DeliberationId);
            activity?.SetTag(CouncilTelemetry.Tags.MotionId, deliberation.Motion.MotionId);
            activity?.SetTag("council.question_length", request.Question.Length);

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

        app.MapGet("/api/deliberations/{id}/stream", async (string id, HttpContext context, IDeliberationEventBus bus, ILogger<Program> logger) =>
        {
            context.Response.Headers.CacheControl = "no-cache";
            context.Response.Headers.Connection = "keep-alive";
            context.Response.Headers["X-Accel-Buffering"] = "no";
            context.Response.ContentType = "text/event-stream";

            logger.LogInformation("SSE stream connected for {DeliberationId} from {Origin}", id, context.Request.Headers.Origin.ToString());
            context.RequestAborted.Register(() =>
                logger.LogInformation("SSE stream disconnected for {DeliberationId}", id));

            await using var subscription = bus.Subscribe(id);

            await SseWriter.WriteEventAsync(context, "stream.ready", "{}", eventId: null, context.RequestAborted);

            var pingInterval = TimeSpan.FromSeconds(15);
            var abort = context.RequestAborted;

            try
            {
                while (!abort.IsCancellationRequested)
                {
                    var waitToReadTask = subscription.Reader.WaitToReadAsync(abort).AsTask();
                    var completed = await Task.WhenAny(waitToReadTask, Task.Delay(pingInterval, abort));

                    if (completed != waitToReadTask)
                    {
                        await SseWriter.WriteEventAsync(context, "stream.ping", "{}", eventId: null, abort);
                        continue;
                    }

                    if (!await waitToReadTask)
                    {
                        break;
                    }

                    while (subscription.Reader.TryRead(out var ev))
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
                        await SseWriter.WriteEventAsync(context, eventName, data, ev.EventId.ToString(), abort);
                    }
                }
            }
            catch (OperationCanceledException) when (abort.IsCancellationRequested)
            {
                // Expected on client disconnect.
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "SSE stream failed for {DeliberationId}", id);
                throw;
            }
        });
    }
}

sealed record DebugConfigResponse
{
    public required string Environment { get; init; }
    public required string ApplicationName { get; init; }
    public string? AssemblyVersion { get; init; }
    public string? AspNetCoreUrls { get; init; }
    public string? OtlpEndpoint { get; init; }
    public required DebugAgentConfig Agents { get; init; }
}

sealed record DebugAgentConfig
{
    public required bool AzureOpenAiConfigured { get; init; }
    public string? AzureOpenAiEndpointHost { get; init; }
    public string? AzureOpenAiDeployment { get; init; }
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
