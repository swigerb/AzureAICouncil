using System.Text;

namespace AzureAiCouncil.Api.Streaming;

public static class SseWriter
{
    public static async Task WriteEventAsync(HttpContext context, string eventName, string jsonData, string? eventId, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(eventId))
        {
            sb.Append("id: ").Append(eventId).Append('\n');
        }

        sb.Append("event: ").Append(eventName).Append('\n');

        // SSE 'data:' must be single-line or repeated. Keep it simple: normalize newlines.
        var normalized = jsonData.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\n", "\\n", StringComparison.Ordinal);
        sb.Append("data: ").Append(normalized).Append("\n\n");

        await context.Response.WriteAsync(sb.ToString(), Encoding.UTF8, cancellationToken);
        await context.Response.Body.FlushAsync(cancellationToken);
    }
}
