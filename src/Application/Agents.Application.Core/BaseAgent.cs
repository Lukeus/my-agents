using Agents.Domain.Core.Events;
using Agents.Domain.Core.Interfaces;
using Agents.Infrastructure.Prompts.Services;
using Agents.Infrastructure.Prompts.Models;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.Diagnostics;

namespace Agents.Application.Core;

/// <summary>
/// Abstract base class for all AI agents with Semantic Kernel integration
/// </summary>
public abstract class BaseAgent
{
    protected readonly ILLMProvider LLMProvider;
    protected readonly IPromptLoader PromptLoader;
    protected readonly IEventPublisher EventPublisher;
    protected readonly ILogger Logger;
    protected readonly string AgentName;

    protected BaseAgent(
        ILLMProvider llmProvider,
        IPromptLoader promptLoader,
        IEventPublisher eventPublisher,
        ILogger logger,
        string agentName)
    {
        LLMProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
        PromptLoader = promptLoader ?? throw new ArgumentNullException(nameof(promptLoader));
        EventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        AgentName = agentName ?? throw new ArgumentNullException(nameof(agentName));
    }

    /// <summary>
    /// Executes the agent with the given input and context
    /// </summary>
    public async Task<AgentResult> ExecuteAsync(string input, AgentContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            Logger.LogInformation(
                "Agent {AgentName} starting execution. ExecutionId: {ExecutionId}, CorrelationId: {CorrelationId}",
                AgentName, context.ExecutionId, context.CorrelationId);

            var result = await ExecuteCoreAsync(input, context);

            stopwatch.Stop();
            // Update duration in metadata since AgentResult is not a record
            result.Metadata["Duration"] = stopwatch.Elapsed;

            Logger.LogInformation(
                "Agent {AgentName} completed execution in {Duration}ms. Success: {IsSuccess}",
                AgentName, stopwatch.ElapsedMilliseconds, result.IsSuccess);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex,
                "Agent {AgentName} failed with exception. ExecutionId: {ExecutionId}",
                AgentName, context.ExecutionId);

            return AgentResult.Failure(
                $"Agent execution failed: {ex.Message}",
                new Dictionary<string, object>
                {
                    ["Exception"] = ex.GetType().Name,
                    ["Duration"] = stopwatch.Elapsed
                });
        }
    }

    /// <summary>
    /// Core execution logic implemented by derived agents
    /// </summary>
    protected abstract Task<AgentResult> ExecuteCoreAsync(string input, AgentContext context);

    /// <summary>
    /// Loads a prompt by name and renders it with the given variables
    /// </summary>
    protected async Task<string> LoadPromptAsync(string promptName, Dictionary<string, object> variables)
    {
        var prompt = await PromptLoader.LoadPromptAsync(promptName);
        if (prompt == null)
        {
            throw new InvalidOperationException($"Prompt '{promptName}' not found");
        }

        return RenderPrompt(prompt.Content, variables);
    }

    /// <summary>
    /// Invokes the LLM with the given prompt using Semantic Kernel
    /// </summary>
    protected async Task<string> InvokeKernelAsync(
        string promptText,
        KernelArguments? arguments = null,
        CancellationToken cancellationToken = default)
    {
        var kernel = LLMProvider.GetKernel();

        var result = await kernel.InvokePromptAsync(
            promptText,
            arguments,
            cancellationToken: cancellationToken);

        return result.ToString();
    }

    /// <summary>
    /// Publishes a domain event
    /// </summary>
    protected async Task PublishEventAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        await EventPublisher.PublishAsync(domainEvent, cancellationToken);

        Logger.LogInformation(
            "Agent {AgentName} published event {EventType}",
            AgentName, domainEvent.GetType().Name);
    }

    /// <summary>
    /// Simple template rendering (replace {{variable}} with values)
    /// </summary>
    private string RenderPrompt(string template, Dictionary<string, object> variables)
    {
        var result = template;
        foreach (var (key, value) in variables)
        {
            result = result.Replace($"{{{{{key}}}}}", value?.ToString() ?? string.Empty);
        }
        return result;
    }
}
