using System.Text.RegularExpressions;
using Agents.Infrastructure.Prompts.Models;

namespace Agents.Infrastructure.Prompts.Services;

/// <summary>
/// Validates prompts and their inputs against defined schemas.
/// </summary>
public class PromptValidator
{
    /// <summary>
    /// Validates a prompt's metadata and structure.
    /// </summary>
    public ValidationResult ValidatePrompt(Prompt prompt)
    {
        var result = new ValidationResult();

        // Validate metadata
        if (string.IsNullOrWhiteSpace(prompt.Metadata.Name))
        {
            result.AddError("Prompt name is required");
        }

        if (string.IsNullOrWhiteSpace(prompt.Metadata.Version))
        {
            result.AddError("Prompt version is required");
        }
        else if (!IsValidSemanticVersion(prompt.Metadata.Version))
        {
            result.AddError($"Invalid semantic version: {prompt.Metadata.Version}");
        }

        if (string.IsNullOrWhiteSpace(prompt.Metadata.Description))
        {
            result.AddError("Prompt description is required");
        }

        if (string.IsNullOrWhiteSpace(prompt.Content))
        {
            result.AddError("Prompt content is required");
        }

        // Validate template variables match schema
        if (prompt.Metadata.InputSchema != null)
        {
            var templateVars = prompt.GetTemplateVariables();
            var schemaParams = prompt.Metadata.InputSchema.Select(p => p.Name).ToHashSet();

            foreach (var templateVar in templateVars)
            {
                if (!schemaParams.Contains(templateVar))
                {
                    result.AddWarning($"Template variable '{{{{{templateVar}}}}}' not defined in input schema");
                }
            }

            foreach (var schemaParam in prompt.Metadata.InputSchema.Where(p => p.Required))
            {
                if (!templateVars.Contains(schemaParam.Name))
                {
                    result.AddWarning($"Required parameter '{schemaParam.Name}' from schema not used in template");
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Validates input values against prompt schema.
    /// </summary>
    public ValidationResult ValidateInputs(Prompt prompt, Dictionary<string, object> inputs)
    {
        var result = new ValidationResult();

        if (prompt.Metadata.InputSchema == null)
        {
            return result;
        }

        foreach (var param in prompt.Metadata.InputSchema)
        {
            if (!inputs.ContainsKey(param.Name))
            {
                if (param.Required && param.Default == null)
                {
                    result.AddError($"Required parameter '{param.Name}' is missing");
                }
                continue;
            }

            var value = inputs[param.Name];
            var paramResult = ValidateParameter(param, value);
            result.Merge(paramResult);
        }

        return result;
    }

    /// <summary>
    /// Validates a single parameter value against its schema definition.
    /// </summary>
    private ValidationResult ValidateParameter(PromptParameter param, object? value)
    {
        var result = new ValidationResult();

        if (value == null)
        {
            if (param.Required && param.Default == null)
            {
                result.AddError($"Parameter '{param.Name}' cannot be null");
            }
            return result;
        }

        switch (param.Type.ToLowerInvariant())
        {
            case "string":
                ValidateString(param, value.ToString(), result);
                break;

            case "number":
            case "integer":
                ValidateNumber(param, value, result);
                break;

            case "boolean":
                if (value is not bool)
                {
                    result.AddError($"Parameter '{param.Name}' must be a boolean");
                }
                break;

            case "enum":
                ValidateEnum(param, value.ToString(), result);
                break;

            case "array":
                // TODO: Implement array validation
                break;

            case "object":
                // TODO: Implement nested object validation
                break;

            default:
                result.AddWarning($"Unknown parameter type '{param.Type}' for '{param.Name}'");
                break;
        }

        return result;
    }

    private void ValidateString(PromptParameter param, string? value, ValidationResult result)
    {
        if (string.IsNullOrEmpty(value))
        {
            if (param.Required)
            {
                result.AddError($"Parameter '{param.Name}' cannot be empty");
            }
            return;
        }

        if (param.MinLength.HasValue && value.Length < param.MinLength.Value)
        {
            result.AddError($"Parameter '{param.Name}' must be at least {param.MinLength} characters");
        }

        if (param.MaxLength.HasValue && value.Length > param.MaxLength.Value)
        {
            result.AddError($"Parameter '{param.Name}' must be at most {param.MaxLength} characters");
        }

        if (!string.IsNullOrEmpty(param.Pattern))
        {
            try
            {
                if (!Regex.IsMatch(value, param.Pattern))
                {
                    result.AddError($"Parameter '{param.Name}' does not match required pattern: {param.Pattern}");
                }
            }
            catch (Exception ex)
            {
                result.AddError($"Invalid regex pattern for '{param.Name}': {ex.Message}");
            }
        }
    }

    private void ValidateNumber(PromptParameter param, object value, ValidationResult result)
    {
        if (!double.TryParse(value.ToString(), out var numValue))
        {
            result.AddError($"Parameter '{param.Name}' must be a valid number");
            return;
        }

        if (param.Min.HasValue && numValue < param.Min.Value)
        {
            result.AddError($"Parameter '{param.Name}' must be at least {param.Min}");
        }

        if (param.Max.HasValue && numValue > param.Max.Value)
        {
            result.AddError($"Parameter '{param.Name}' must be at most {param.Max}");
        }
    }

    private void ValidateEnum(PromptParameter param, string? value, ValidationResult result)
    {
        if (param.Values == null || param.Values.Count == 0)
        {
            result.AddError($"Enum parameter '{param.Name}' has no allowed values defined");
            return;
        }

        if (string.IsNullOrEmpty(value) || !param.Values.Contains(value))
        {
            result.AddError(
                $"Parameter '{param.Name}' must be one of: {string.Join(", ", param.Values)}");
        }
    }

    private static bool IsValidSemanticVersion(string version)
    {
        // Basic semantic version validation (major.minor.patch)
        var pattern = @"^\d+\.\d+\.\d+(-[a-zA-Z0-9.-]+)?(\+[a-zA-Z0-9.-]+)?$";
        return Regex.IsMatch(version, pattern);
    }
}

/// <summary>
/// Result of a validation operation.
/// </summary>
public class ValidationResult
{
    private readonly List<string> _errors = new();
    private readonly List<string> _warnings = new();

    public IReadOnlyList<string> Errors => _errors.AsReadOnly();
    public IReadOnlyList<string> Warnings => _warnings.AsReadOnly();

    public bool IsValid => _errors.Count == 0;
    public bool HasWarnings => _warnings.Count > 0;

    public void AddError(string error)
    {
        _errors.Add(error);
    }

    public void AddWarning(string warning)
    {
        _warnings.Add(warning);
    }

    public void Merge(ValidationResult other)
    {
        _errors.AddRange(other.Errors);
        _warnings.AddRange(other.Warnings);
    }

    public override string ToString()
    {
        var parts = new List<string>();

        if (_errors.Any())
        {
            parts.Add($"Errors: {string.Join("; ", _errors)}");
        }

        if (_warnings.Any())
        {
            parts.Add($"Warnings: {string.Join("; ", _warnings)}");
        }

        return parts.Any() ? string.Join(" | ", parts) : "Valid";
    }
}
