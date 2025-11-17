using Agents.Domain.Core.Events;
using Agents.Domain.Core.Interfaces;
using Agents.Infrastructure.Prompts.Services;
using Agents.Infrastructure.Prompts.Models;
using Agents.Shared.Security;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Polly;
using Polly.Retry;
using Polly.Timeout;
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
    protected readonly IInputSanitizer InputSanitizer;
    protected readonly string AgentName;
    private readonly ResiliencePipeline _llmResiliencePipeline;

    protected BaseAgent(
        ILLMProvider llmProvider,
        IPromptLoader promptLoader,
        IEventPublisher eventPublisher,
        ILogger logger,
        IInputSanitizer inputSanitizer,
        string agentName)
    {
        LLMProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
        PromptLoader = promptLoader ?? throw new ArgumentNullException(nameof(promptLoader));
        EventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        InputSanitizer = inputSanitizer ?? throw new ArgumentNullException(nameof(inputSanitizer));
        AgentName = agentName ?? throw new ArgumentNullException(nameof(agentName));

        // Configure LLM resilience pipeline with retry and timeout
        _llmResiliencePipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 2,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                OnRetry = args =>
                {
                    Logger.LogWarning(
                        args.Outcome.Exception,
                        "LLM call retry {RetryCount} for agent {AgentName} after {Delay}ms",
                        args.AttemptNumber,
                        AgentName,
                        args.RetryDelay.TotalMilliseconds);
                    return ValueTask.CompletedTask;
                }
            })
            .AddTimeout(TimeSpan.FromSeconds(30)) // 30 second timeout for LLM calls
            .Build();
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

            // Enhanced logging with comprehensive context
            Logger.LogError(ex,
                "Agent {AgentName} failed. ExecutionId: {ExecutionId}, CorrelationId: {CorrelationId}, " +
                "InitiatedBy: {InitiatedBy}, Duration: {Duration}ms, Input: {InputPreview}",
                AgentName,
                context.ExecutionId,
                context.CorrelationId,
                context.InitiatedBy ?? "Unknown",
                stopwatch.ElapsedMilliseconds,
                input.Length > 100 ? input.Substring(0, 100) + "..." : input);

            return AgentResult.Failure(
                $"Agent execution failed: {ex.Message}",
                new Dictionary<string, object>
                {
                    ["Exception"] = ex.GetType().FullName ?? ex.GetType().Name,
                    ["ExceptionMessage"] = ex.Message,
                    ["InnerException"] = (object?)ex.InnerException?.Message ?? string.Empty,
                    ["StackTrace"] = (object?)ex.StackTrace ?? string.Empty,
                    ["Duration"] = stopwatch.Elapsed,
                    ["CorrelationId"] = context.CorrelationId,
                    ["ExecutionId"] = context.ExecutionId,
                    ["AgentName"] = AgentName
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
    /// Invokes the LLM with the given prompt using Semantic Kernel with resilience policies
    /// </summary>
    protected async Task<string> InvokeKernelAsync(
        string promptText,
        KernelArguments? arguments = null,
        CancellationToken cancellationToken = default)
    {
        var kernel = LLMProvider.GetKernel();

        // Execute with retry and timeout policies
        var result = await _llmResiliencePipeline.ExecuteAsync(async ct =>
        {
            return await kernel.InvokePromptAsync(
                promptText,
                arguments,
                cancellationToken: ct);
        }, cancellationToken);

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
    /// Simple template rendering with input sanitization (replace {{variable}} with sanitized values)
    /// </summary>
    private string RenderPrompt(string template, Dictionary<string, object> variables)
    {
        var result = template;
        foreach (var (key, value) in variables)
        {
            var rawValue = value?.ToString() ?? string.Empty;
            var sanitizedValue = InputSanitizer.Sanitize(rawValue);

            // Log warning if injection patterns were detected
            if (InputSanitizer.ContainsInjectionPatterns(rawValue))
            {
                Logger.LogWarning(
                    "Potential injection patterns detected in prompt variable '{Key}'. Input has been sanitized. " +
                    "Agent: {AgentName}",
                    key, AgentName);
            }

            result = result.Replace($"{{{{{key}}}}}", sanitizedValue);
        }
        return result;
    }
}
