using System.Text;
using System.Text.RegularExpressions;

namespace AzureAiCouncil.Agents;

internal static partial class MermaidSanitizer
{
    // Mermaid can be picky about punctuation inside bracket labels depending on parser/version.
    // Normalizing everything to the quoted-label form tends to be the most portable.
    private static readonly Regex BracketLabelRegex = new(@"\b([A-Za-z_][A-Za-z0-9_]*)\[(?<label>[^\]]*)\]", RegexOptions.Compiled);

    public static string Sanitize(string snippet)
    {
        if (string.IsNullOrWhiteSpace(snippet))
        {
            return snippet;
        }

        var trimmed = snippet.Trim();

        // Handle fenced blocks if present.
        var fenceStart = trimmed.IndexOf("```mermaid", StringComparison.OrdinalIgnoreCase);
        if (fenceStart >= 0)
        {
            var fenceContentStart = trimmed.IndexOf('\n', fenceStart);
            if (fenceContentStart < 0)
            {
                // Not a proper fenced block.
                return SanitizeGraphText(trimmed);
            }

            fenceContentStart++;
            var fenceEnd = trimmed.IndexOf("```", fenceContentStart, StringComparison.Ordinal);
            if (fenceEnd < 0)
            {
                return SanitizeGraphText(trimmed);
            }

            var before = trimmed.Substring(0, fenceContentStart);
            var inner = trimmed.Substring(fenceContentStart, fenceEnd - fenceContentStart);
            var after = trimmed.Substring(fenceEnd);

            var sanitizedInner = SanitizeGraphText(inner);
            return before + sanitizedInner + after;
        }

        return SanitizeGraphText(trimmed);
    }

    private static string SanitizeGraphText(string text)
    {
        // Remove trailing semicolons on lines (Mermaid doesn’t require them and some parsers choke on them in certain constructs).
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var sb = new StringBuilder(text.Length);

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd();
            if (line.EndsWith(';'))
            {
                line = line.TrimEnd(';').TrimEnd();
            }

            // Convert any node/subgraph bracket labels to quoted form: X[Label] -> X["Label"].
            line = BracketLabelRegex.Replace(line, static m =>
            {
                var id = m.Groups[1].Value;
                var label = m.Groups["label"].Value.Trim();

                // Avoid embedding raw double-quotes inside labels.
                label = label.Replace('"', '\'');

                return $"{id}[\"{label}\"]";
            });

            sb.Append(line);
            if (i < lines.Length - 1)
            {
                sb.Append('\n');
            }
        }

        return sb.ToString();
    }
}
