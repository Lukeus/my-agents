namespace Agents.Infrastructure.Prompts.Models;

/// <summary>
/// Represents a complete prompt with metadata and content.
/// </summary>
public class Prompt
{
    /// <summary>
    /// Prompt metadata (parsed from YAML frontmatter).
    /// </summary>
    public required PromptMetadata Metadata { get; set; }

    /// <summary>
    /// The actual prompt content/template.
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// File path where the prompt was loaded from.
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// SHA-256 hash of the prompt content for change detection.
    /// </summary>
    public string? ContentHash { get; set; }

    /// <summary>
    /// When the prompt was loaded into memory.
    /// </summary>
    public DateTime LoadedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Renders the prompt by replacing template variables.
    /// </summary>
    /// <param name="variables">Dictionary of variable names to values.</param>
    /// <returns>Rendered prompt content.</returns>
    public string Render(Dictionary<string, string> variables)
    {
        var rendered = Content;

        foreach (var (key, value) in variables)
        {
            // Support {{variable}} syntax
            rendered = rendered.Replace($"{{{{{key}}}}}", value);
        }

        return rendered;
    }

    /// <summary>
    /// Validates that all required input parameters are provided.
    /// </summary>
    /// <param name="variables">Variables being passed to the prompt.</param>
    /// <returns>List of validation errors, empty if valid.</returns>
    public List<string> ValidateInputs(Dictionary<string, string> variables)
    {
        var errors = new List<string>();

        if (Metadata.InputSchema == null)
        {
            return errors;
        }

        foreach (var param in Metadata.InputSchema.Where(p => p.Required))
        {
            if (!variables.ContainsKey(param.Name))
            {
                errors.Add($"Required parameter '{param.Name}' is missing");
            }
        }

        return errors;
    }

    /// <summary>
    /// Gets all template variables used in the prompt content.
    /// </summary>
    public List<string> GetTemplateVariables()
    {
        var variables = new List<string>();
        var content = Content;
        var startIndex = 0;

        while (true)
        {
            startIndex = content.IndexOf("{{", startIndex, StringComparison.Ordinal);
            if (startIndex == -1)
            {
                break;
            }

            var endIndex = content.IndexOf("}}", startIndex, StringComparison.Ordinal);
            if (endIndex == -1)
            {
                break;
            }

            var variable = content.Substring(startIndex + 2, endIndex - startIndex - 2).Trim();
            if (!variables.Contains(variable))
            {
                variables.Add(variable);
            }

            startIndex = endIndex + 2;
        }

        return variables;
    }
}
