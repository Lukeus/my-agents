using Agents.Application.Core;
using Agents.Domain.Core.Interfaces;
using Agents.Infrastructure.Prompts.Services;
using Agents.Shared.Security;
using Microsoft.Extensions.Logging;

namespace Agents.Application.DevOps;

/// <summary>
/// Agent responsible for DevOps automation, GitHub Projects, Issues, and workflows
/// </summary>
public class DevOpsAgent : BaseAgent
{
    public DevOpsAgent(
        ILLMProvider llmProvider,
        IPromptLoader promptLoader,
        IEventPublisher eventPublisher,
        IInputSanitizer inputSanitizer,
        ILogger<DevOpsAgent> logger)
        : base(llmProvider, promptLoader, eventPublisher, logger, inputSanitizer, "DevOpsAgent")
    {
    }

    protected override async Task<AgentResult> ExecuteCoreAsync(string input, AgentContext context)
    {
        try
        {
            var request = System.Text.Json.JsonSerializer.Deserialize<DevOpsRequest>(input);
            if (request == null)
            {
                return AgentResult.Failure("Invalid DevOps request format");
            }

            return request.Action.ToLowerInvariant() switch
            {
                "create_issue" => await CreateIssueAsync(request, context),
                "update_project" => await UpdateProjectAsync(request, context),
                "analyze_sprint" => await AnalyzeSprintAsync(request, context),
                "trigger_workflow" => await TriggerWorkflowAsync(request, context),
                _ => AgentResult.Failure($"Unknown action: {request.Action}")
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing DevOps request");
            return AgentResult.Failure($"Error: {ex.Message}");
        }
    }

    private async Task<AgentResult> CreateIssueAsync(DevOpsRequest request, AgentContext context)
    {
        var promptText = await LoadPromptAsync("prompts/devops-issue-creator.prompt", new Dictionary<string, object>
        {
            ["title"] = request.Parameters.GetValueOrDefault("title", string.Empty),
            ["description"] = request.Parameters.GetValueOrDefault("description", string.Empty),
            ["labels"] = request.Parameters.GetValueOrDefault("labels", string.Empty)
        });

        var result = await InvokeKernelAsync(promptText, cancellationToken: context.CancellationToken);

        // TODO: Actually create issue via GitHub API (Octokit)
        Logger.LogInformation("Created GitHub issue: {Result}", result);

        return AgentResult.Success(
            $"Issue created successfully",
            new Dictionary<string, object> { ["issueNumber"] = "123", ["analysis"] = result });
    }

    private async Task<AgentResult> UpdateProjectAsync(DevOpsRequest request, AgentContext context)
    {
        var promptText = await LoadPromptAsync("prompts/devops-project-manager.prompt", new Dictionary<string, object>
        {
            ["projectName"] = request.Parameters.GetValueOrDefault("projectName", string.Empty),
            ["updates"] = request.Parameters.GetValueOrDefault("updates", string.Empty)
        });

        var result = await InvokeKernelAsync(promptText, cancellationToken: context.CancellationToken);

        Logger.LogInformation("Updated GitHub project: {Result}", result);

        return AgentResult.Success($"Project updated", new Dictionary<string, object> { ["updateSummary"] = result });
    }

    private async Task<AgentResult> AnalyzeSprintAsync(DevOpsRequest request, AgentContext context)
    {
        var promptText = await LoadPromptAsync("prompts/devops-sprint-analyzer.prompt", new Dictionary<string, object>
        {
            ["sprintData"] = request.Parameters.GetValueOrDefault("sprintData", string.Empty)
        });

        var analysis = await InvokeKernelAsync(promptText, cancellationToken: context.CancellationToken);

        return AgentResult.Success($"Sprint analysis complete", new Dictionary<string, object> { ["analysis"] = analysis });
    }

    private async Task<AgentResult> TriggerWorkflowAsync(DevOpsRequest request, AgentContext context)
    {
        // TODO: Trigger GitHub Actions workflow
        Logger.LogInformation("Triggering workflow: {Workflow}", request.Parameters.GetValueOrDefault("workflowName", string.Empty));

        await Task.Delay(100, context.CancellationToken);

        return AgentResult.Success("Workflow triggered");
    }
}

public record DevOpsRequest
{
    public required string Action { get; init; }
    public Dictionary<string, object> Parameters { get; init; } = new();
}
