using System.Collections.Concurrent;

namespace AzureAiCouncil.Core.Services;

/// <summary>
/// Renders prompt templates with variable substitution.
/// </summary>
public sealed class PromptRenderer
{
    private readonly ConcurrentDictionary<string, string> _cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Reads and renders a prompt template from disk, caching the template for reuse.
    /// </summary>
    /// <param name="promptPath">Absolute path to the prompt template file.</param>
    /// <param name="variables">Variables to substitute in the template.</param>
    /// <returns>The rendered prompt text.</returns>
    public string RenderFromFile(string promptPath, IReadOnlyDictionary<string, string> variables)
    {
        var template = _cache.GetOrAdd(promptPath, static path => File.ReadAllText(path));
        return Render(template, variables);
    }

    /// <summary>
    /// Renders a template string by substituting {{key}} placeholders with values.
    /// </summary>
    /// <param name="template">The template text.</param>
    /// <param name="variables">Variables to substitute.</param>
    /// <returns>The rendered text.</returns>
    public static string Render(string template, IReadOnlyDictionary<string, string> variables)
    {
        var result = template;
        foreach (var (key, value) in variables)
        {
            result = result.Replace("{{" + key + "}}", value, StringComparison.Ordinal);
        }

        return result;
    }
}
