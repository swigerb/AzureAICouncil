using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AzureAiCouncil.ApiTests;

public sealed class FakeOpenAiServerFixture : IAsyncLifetime
{
    private WebApplication? _app;

    private readonly ConcurrentQueue<string> _bodies = new();

    public Uri BaseAddress { get; private set; } = new Uri("http://127.0.0.1:0");

    public int RequestCount => _bodies.Count;

    public IReadOnlyCollection<string> Bodies => _bodies.ToArray();

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Development",
        });

        builder.WebHost.UseKestrel();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        var app = builder.Build();

        app.MapPost("/openai/v1/{**path}", async (HttpContext ctx) =>
        {
            using var reader = new StreamReader(ctx.Request.Body);
            var body = await reader.ReadToEndAsync();
            _bodies.Enqueue(body);

            var assistantJson = CreateAssistantJson(body);

            var payload = new
            {
                id = "chatcmpl_test",
                @object = "chat.completion",
                created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                model = "test",
                choices = new[]
                {
                    new
                    {
                        index = 0,
                        message = new { role = "assistant", content = assistantJson },
                        finish_reason = "stop",
                    }
                },
            };

            return Results.Json(payload);
        });

        await app.StartAsync();

        var address = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()?.Addresses.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(address))
        {
            await app.DisposeAsync();
            throw new InvalidOperationException("Failed to determine fake OpenAI server listening address.");
        }

        _app = app;
        BaseAddress = new Uri(address);
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

    private static string CreateAssistantJson(string requestBody)
    {
        if (requestBody.Contains("You normalize user requests into a Motion object.", StringComparison.OrdinalIgnoreCase))
        {
            return JsonSerializer.Serialize(new
            {
                question = "Normalized question",
                constraints = new
                {
                    budget = "medium",
                    timeline = "4 weeks",
                    technologyPreferences = Array.Empty<string>(),
                }
            });
        }

        if (requestBody.Contains("You synthesize multi-agent deliberations into a single resolution.", StringComparison.OrdinalIgnoreCase))
        {
            return JsonSerializer.Serialize(new
            {
                deliberationId = "placeholder",
                summary = "Summary from fake model",
                recommendation = "Proceed",
                pros = new[] { "Fast iteration" },
                cons = new[] { "Some risk" },
                scores = new { risk = "Medium", cost = "Low", complexity = "Medium", confidence = 0.75 },
                whenThisIsAGoodIdea = new[] { "Internal service" },
                whenToAvoid = new[] { "Hard compliance requirements" },
                suggestedNextSteps = new[] { "Prototype", "Measure" },
                mermaidArchitectureSnippet = "graph LR; Client-->API;"
            });
        }

        var councilor = ExtractCouncilor(requestBody);
        var round = ExtractRound(requestBody);

        if (councilor is not null && round is not null)
        {
            var councilorEnumValue = councilor switch
            {
                "Vision" => 0,
                "Risk" => 1,
                "Architecture" => 2,
                "Security" => 3,
                "Finance" => 4,
                _ => 0,
            };

            return JsonSerializer.Serialize(new
            {
                councilor = councilorEnumValue,
                round,
                stanceTitle = $"{councilor} view",
                answer = "Answer",
                rationale = "Rationale",
                risksOrConcerns = new[] { "Risk" },
                costTier = "Low",
                complexityTier = "Medium",
                confidence = 0.7,
                position = new { goNoGo = "Go", priority = "Medium" },
                notesForCouncil = new[] { "Note" },
                mermaidHint = (string?)null,
            });
        }

        return "{}";
    }

    private static string? ExtractCouncilor(string requestBody)
    {
        // The prompty templates include: "You are the Councilor of Vision." and also
        // "councilor must be \"Vision\"." (the quotes appear JSON-escaped in the request body).
        foreach (var kind in new[] { "Vision", "Risk", "Architecture", "Security", "Finance" })
        {
            if (requestBody.Contains($"You are the Councilor of {kind}", StringComparison.OrdinalIgnoreCase))
            {
                return kind;
            }

            // Match the rule line inside JSON strings, where quotes are escaped.
            if (requestBody.Contains($"councilor must be \\\"{kind}\\\"", StringComparison.OrdinalIgnoreCase))
            {
                return kind;
            }
        }

        return null;
    }

    private static int? ExtractRound(string requestBody)
    {
        // The prompty templates include: round must be {{Round}}. The rendered prompt includes "Round X".
        for (var i = 1; i <= 3; i++)
        {
            if (requestBody.Contains($"Round {i}", StringComparison.OrdinalIgnoreCase)
                || requestBody.Contains($"round must be {i}", StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return null;
    }
}
