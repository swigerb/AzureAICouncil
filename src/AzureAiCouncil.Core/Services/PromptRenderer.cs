using System.Collections.Concurrent;

namespace AzureAiCouncil.Core.Services;

public sealed class PromptRenderer
{
    private readonly ConcurrentDictionary<string, string> _cache = new(StringComparer.OrdinalIgnoreCase);

    public string RenderFromFile(string promptPath, IReadOnlyDictionary<string, string> variables)
    {
        var template = _cache.GetOrAdd(promptPath, static path => File.ReadAllText(path));
        return Render(template, variables);
    }

    public static string Render(string template, IReadOnlyDictionary<string, string> variables)
    {
        var result = template;
        foreach (var kvp in variables)
        {
            result = result.Replace("{{" + kvp.Key + "}}", kvp.Value, StringComparison.Ordinal);
        }

        return result;
    }
}
