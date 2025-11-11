namespace Agents.Infrastructure.Prompts.Models;

/// <summary>
/// Defines the schema for prompt inputs and outputs with validation rules.
/// </summary>
public class PromptSchema
{
    /// <summary>
    /// Input parameter definitions.
    /// </summary>
    public List<PromptParameter> InputSchema { get; set; } = new();

    /// <summary>
    /// Output schema definition.
    /// </summary>
    public PromptOutputSchema? OutputSchema { get; set; }
}

/// <summary>
/// Defines a single prompt parameter with validation rules.
/// </summary>
public class PromptParameter
{
    /// <summary>
    /// Parameter name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Parameter type (string, number, boolean, enum, object, array).
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// Whether the parameter is required.
    /// </summary>
    public bool Required { get; set; } = true;

    /// <summary>
    /// Description of the parameter.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Default value if not provided.
    /// </summary>
    public object? Default { get; set; }

    /// <summary>
    /// Allowed values for enum types.
    /// </summary>
    public List<string>? Values { get; set; }

    /// <summary>
    /// Minimum length for string types.
    /// </summary>
    public int? MinLength { get; set; }

    /// <summary>
    /// Maximum length for string types.
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// Minimum value for number types.
    /// </summary>
    public double? Min { get; set; }

    /// <summary>
    /// Maximum value for number types.
    /// </summary>
    public double? Max { get; set; }

    /// <summary>
    /// Regular expression pattern for string validation.
    /// </summary>
    public string? Pattern { get; set; }

    /// <summary>
    /// Nested schema for object types.
    /// </summary>
    public List<PromptParameter>? Properties { get; set; }
}

/// <summary>
/// Defines the expected output schema from a prompt.
/// </summary>
public class PromptOutputSchema
{
    /// <summary>
    /// Output type (object, string, array, etc.).
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// Properties for object type outputs.
    /// </summary>
    public Dictionary<string, PromptOutputProperty>? Properties { get; set; }

    /// <summary>
    /// Description of the output.
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Defines a property in the output schema.
/// </summary>
public class PromptOutputProperty
{
    /// <summary>
    /// Property type.
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// Description of the property.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether the property is required.
    /// </summary>
    public bool Required { get; set; } = true;
}
