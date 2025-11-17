namespace Agents.Shared.Security;

/// <summary>
/// Interface for sanitizing user input to prevent injection attacks
/// </summary>
public interface IInputSanitizer
{
    /// <summary>
    /// Sanitizes input to remove or escape potentially malicious content
    /// </summary>
    /// <param name="input">The raw user input</param>
    /// <returns>Sanitized input safe for use in prompts</returns>
    string Sanitize(string input);

    /// <summary>
    /// Checks if input contains potentially malicious patterns
    /// </summary>
    /// <param name="input">The input to check</param>
    /// <returns>True if input contains injection patterns, false otherwise</returns>
    bool ContainsInjectionPatterns(string input);
}
