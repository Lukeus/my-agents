using System.Text.RegularExpressions;

namespace Agents.Shared.Security;

/// <summary>
/// Default implementation of input sanitization for LLM prompt protection
/// </summary>
public class InputSanitizer : IInputSanitizer
{
    private static readonly string[] _dangerousPatterns = new[]
    {
        // Markdown code blocks that could manipulate LLM context
        "```",
        
        // Common LLM instruction keywords
        "SYSTEM:",
        "SYSTEM PROMPT:",
        "ASSISTANT:",
        "USER:",
        "INSTRUCTION:",
        "INSTRUCTIONS:",
        
        // Prompt injection attempts
        "IGNORE PREVIOUS",
        "IGNORE ALL PREVIOUS",
        "DISREGARD",
        "DISREGARD ALL",
        "FORGET",
        "OVERRIDE",
        "NEW INSTRUCTIONS:",
        
        // Role manipulation
        "YOU ARE NOW",
        "ACT AS",
        "PRETEND TO BE",
        "ROLEPLAY AS",
        
        // Script injection
        "<script",
        "</script>",
        "javascript:",
        "onerror=",
        "onclick=",
        "onload=",
        
        // Other common attacks
        "eval(",
        "__import__",
        "exec("
    };

    private static readonly Regex _controlCharactersRegex = new(@"[\x00-\x1F\x7F]", RegexOptions.Compiled);
    private static readonly Regex _multipleNewlinesRegex = new(@"\n{4,}", RegexOptions.Compiled);

    /// <inheritdoc/>
    public string Sanitize(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        // Remove control characters (except newline, tab, carriage return)
        var sanitized = _controlCharactersRegex.Replace(input, "");

        // Escape dangerous patterns by adding backslashes
        foreach (var pattern in _dangerousPatterns)
        {
            if (sanitized.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                // Replace with escaped version (add backslash before)
                var regex = new Regex(Regex.Escape(pattern), RegexOptions.IgnoreCase);
                sanitized = regex.Replace(sanitized, m => $"\\{m.Value}");
            }
        }

        // Limit excessive newlines (potential context manipulation)
        sanitized = _multipleNewlinesRegex.Replace(sanitized, "\n\n\n");

        // Trim excessive whitespace
        sanitized = sanitized.Trim();

        return sanitized;
    }

    /// <inheritdoc/>
    public bool ContainsInjectionPatterns(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return false;
        }

        return _dangerousPatterns.Any(pattern =>
            input.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }
}
