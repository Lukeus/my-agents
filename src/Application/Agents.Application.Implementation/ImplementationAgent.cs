using Agents.Application.Core;
using Agents.Domain.Core.Interfaces;
using Agents.Infrastructure.Prompts.Services;
using Agents.Shared.Security;
using Microsoft.Extensions.Logging;

namespace Agents.Application.Implementation;

/// <summary>
/// Agent responsible for code generation, review, and refactoring
/// </summary>
public class ImplementationAgent : BaseAgent
{
    public ImplementationAgent(
        ILLMProvider llmProvider,
        IPromptLoader promptLoader,
        IEventPublisher eventPublisher,
        IInputSanitizer inputSanitizer,
        ILogger<ImplementationAgent> logger)
        : base(llmProvider, promptLoader, eventPublisher, logger, inputSanitizer, "ImplementationAgent")
    {
    }

    protected override async Task<AgentResult> ExecuteCoreAsync(string input, AgentContext context)
    {
        try
        {
            var request = System.Text.Json.JsonSerializer.Deserialize<ImplementationRequest>(input);
            if (request == null)
            {
                return AgentResult.Failure("Invalid implementation request format");
            }

            return request.Action.ToLowerInvariant() switch
            {
                "generate_code" => await GenerateCodeAsync(request, context),
                "review_code" => await ReviewCodeAsync(request, context),
                "suggest_refactoring" => await SuggestRefactoringAsync(request, context),
                _ => AgentResult.Failure($"Unknown action: {request.Action}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in implementation");
            return AgentResult.Failure($"Error: {ex.Message}");
        }
    }

    private async Task<AgentResult> GenerateCodeAsync(ImplementationRequest request, AgentContext context)
    {
        var promptText = await LoadPromptAsync("prompts/implementation-code-generator.prompt", new Dictionary<string, object>
        {
            ["specification"] = request.Specification,
            ["language"] = request.Language ?? "C#",
            ["framework"] = request.Framework ?? ".NET",
            ["patterns"] = request.Patterns ?? "Clean Architecture"
        });

        var generatedCode = await InvokeKernelAsync(promptText, cancellationToken: context.CancellationToken);

        _logger.LogInformation("Generated code for: {Spec}", request.Specification);

        return AgentResult<string>.Success(
            generatedCode,
            "Code generated successfully",
            new Dictionary<string, object>
            {
                ["language"] = request.Language ?? "C#",
                ["linesGenerated"] = generatedCode.Split('\n').Length
            });
    }

    private async Task<AgentResult> ReviewCodeAsync(ImplementationRequest request, AgentContext context)
    {
        var promptText = await LoadPromptAsync("prompts/implementation-code-reviewer.prompt", new Dictionary<string, object>
        {
            ["code"] = request.Specification,
            ["reviewCriteria"] = request.Framework ?? "Best Practices, Security, Performance"
        });

        var review = await InvokeKernelAsync(promptText, cancellationToken: context.CancellationToken);

        return AgentResult<string>.Success(review, "Code review completed");
    }

    private async Task<AgentResult> SuggestRefactoringAsync(ImplementationRequest request, AgentContext context)
    {
        var promptText = await LoadPromptAsync("prompts/implementation-refactoring-suggester.prompt", new Dictionary<string, object>
        {
            ["code"] = request.Specification,
            ["goals"] = request.Patterns ?? "Improve maintainability and testability"
        });

        var suggestions = await InvokeKernelAsync(promptText, cancellationToken: context.CancellationToken);

        return AgentResult<string>.Success(suggestions, "Refactoring suggestions generated");
    }
}

public record ImplementationRequest
{
    public required string Action { get; init; }
    public required string Specification { get; init; }
    public string? Language { get; init; }
    public string? Framework { get; init; }
    public string? Patterns { get; init; }
}
