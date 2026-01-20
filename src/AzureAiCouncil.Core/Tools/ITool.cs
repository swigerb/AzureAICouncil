namespace AzureAiCouncil.Core.Tools;

/// <summary>
/// Represents a tool that can be invoked by councilor agents.
/// </summary>
public interface ITool
{
    /// <summary>
    /// Gets the unique name of the tool.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a description of what the tool does.
    /// </summary>
    string Description { get; }
}
