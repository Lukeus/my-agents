using Agents.Application.Core;
using Agents.Domain.Core.Interfaces;
using Agents.Infrastructure.Prompts.Services;
using Agents.Shared.Security;
using Microsoft.Extensions.Logging;

namespace Agents.Application.TestPlanning;

/// <summary>
/// Agent responsible for generating test specifications and test strategies
/// </summary>
public class TestPlanningAgent : BaseAgent
{
    public TestPlanningAgent(
        ILLMProvider llmProvider,
        IPromptLoader promptLoader,
        IEventPublisher eventPublisher,
        IInputSanitizer inputSanitizer,
        ILogger<TestPlanningAgent> logger)
        : base(llmProvider, promptLoader, eventPublisher, logger, inputSanitizer, "TestPlanningAgent")
    {
    }

    protected override async Task<AgentResult> ExecuteCoreAsync(string input, AgentContext context)
    {
        try
        {
            var request = System.Text.Json.JsonSerializer.Deserialize<TestPlanningRequest>(input);
            if (request == null)
            {
                return AgentResult.Failure("Invalid test planning request format");
            }

            return request.Type.ToLowerInvariant() switch
            {
                "generate_spec" => await GenerateTestSpecAsync(request, context),
                "create_strategy" => await CreateTestStrategyAsync(request, context),
                "analyze_coverage" => await AnalyzeCoverageAsync(request, context),
                _ => AgentResult.Failure($"Unknown type: {request.Type}")
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in test planning");
            return AgentResult.Failure($"Error: {ex.Message}");
        }
    }

    private async Task<AgentResult> GenerateTestSpecAsync(TestPlanningRequest request, AgentContext context)
    {
        var promptText = await LoadPromptAsync("prompts/testplanning-spec-generator.prompt", new Dictionary<string, object>
        {
            ["featureDescription"] = request.FeatureDescription,
            ["requirements"] = request.Requirements ?? string.Empty,
            ["testFramework"] = request.TestFramework ?? "xUnit"
        });

        var testSpec = await InvokeKernelAsync(promptText, cancellationToken: context.CancellationToken);

        Logger.LogInformation("Generated test specification");

        return AgentResult<string>.Success(
            testSpec,
            "Test specification generated successfully",
            new Dictionary<string, object> { ["lineCount"] = testSpec.Split('\n').Length });
    }

    private async Task<AgentResult> CreateTestStrategyAsync(TestPlanningRequest request, AgentContext context)
    {
        var promptText = await LoadPromptAsync("prompts/testplanning-strategy-planner.prompt", new Dictionary<string, object>
        {
            ["projectDescription"] = request.FeatureDescription,
            ["testingGoals"] = request.Requirements ?? string.Empty
        });

        var strategy = await InvokeKernelAsync(promptText, cancellationToken: context.CancellationToken);

        return AgentResult<string>.Success(strategy, "Test strategy created");
    }

    private async Task<AgentResult> AnalyzeCoverageAsync(TestPlanningRequest request, AgentContext context)
    {
        var promptText = await LoadPromptAsync("prompts/testplanning-coverage-analyzer.prompt", new Dictionary<string, object>
        {
            ["codeBase"] = request.FeatureDescription,
            ["existingTests"] = request.Requirements ?? string.Empty
        });

        var analysis = await InvokeKernelAsync(promptText, cancellationToken: context.CancellationToken);

        return AgentResult<string>.Success(analysis, "Coverage analysis complete");
    }
}

public record TestPlanningRequest
{
    public required string Type { get; init; }
    public required string FeatureDescription { get; init; }
    public string? Requirements { get; init; }
    public string? TestFramework { get; init; }
}
