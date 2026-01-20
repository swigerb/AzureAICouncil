using AzureAiCouncil.Core.Services;

namespace AzureAiCouncil.Tests;

public sealed class PromptRendererTests
{
    [Fact]
    public void Render_replaces_single_variable()
    {
        var template = "Hello, {{name}}!";
        var variables = new Dictionary<string, string> { ["name"] = "World" };

        var result = PromptRenderer.Render(template, variables);

        Assert.Equal("Hello, World!", result);
    }

    [Fact]
    public void Render_replaces_multiple_variables()
    {
        var template = "{{greeting}}, {{name}}! Welcome to {{place}}.";
        var variables = new Dictionary<string, string>
        {
            ["greeting"] = "Hi",
            ["name"] = "Alice",
            ["place"] = "Wonderland",
        };

        var result = PromptRenderer.Render(template, variables);

        Assert.Equal("Hi, Alice! Welcome to Wonderland.", result);
    }

    [Fact]
    public void Render_handles_multiple_occurrences_of_same_variable()
    {
        var template = "{{name}} said hello to {{name}}.";
        var variables = new Dictionary<string, string> { ["name"] = "Bob" };

        var result = PromptRenderer.Render(template, variables);

        Assert.Equal("Bob said hello to Bob.", result);
    }

    [Fact]
    public void Render_leaves_unknown_placeholders_intact()
    {
        var template = "Hello, {{name}}! Your order {{orderId}} is ready.";
        var variables = new Dictionary<string, string> { ["name"] = "Carol" };

        var result = PromptRenderer.Render(template, variables);

        Assert.Equal("Hello, Carol! Your order {{orderId}} is ready.", result);
    }

    [Fact]
    public void Render_returns_original_template_when_no_placeholders_exist()
    {
        var template = "No placeholders here.";
        var variables = new Dictionary<string, string> { ["unused"] = "value" };

        var result = PromptRenderer.Render(template, variables);

        Assert.Equal("No placeholders here.", result);
    }

    [Fact]
    public void Render_returns_original_template_with_empty_variables()
    {
        var template = "Hello, {{name}}!";
        var variables = new Dictionary<string, string>();

        var result = PromptRenderer.Render(template, variables);

        Assert.Equal("Hello, {{name}}!", result);
    }

    [Fact]
    public void Render_handles_empty_template()
    {
        var template = "";
        var variables = new Dictionary<string, string> { ["name"] = "Test" };

        var result = PromptRenderer.Render(template, variables);

        Assert.Equal("", result);
    }

    [Fact]
    public void Render_preserves_whitespace_and_newlines()
    {
        var template = """
            Line 1: {{first}}
            Line 2: {{second}}
            """;
        var variables = new Dictionary<string, string>
        {
            ["first"] = "A",
            ["second"] = "B",
        };

        var result = PromptRenderer.Render(template, variables);

        Assert.Contains("Line 1: A", result);
        Assert.Contains("Line 2: B", result);
    }
}
