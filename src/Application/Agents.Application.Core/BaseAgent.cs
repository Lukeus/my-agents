using System.Diagnostics;
using Agents.Domain.Core.Events;
using Agents.Domain.Core.Interfaces;
using Agents.Infrastructure.Prompts.Models;
using Agents.Infrastructure.Prompts.Services;
using Agents.Shared.Security;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Polly;
using Polly.Retry;
using Polly.Timeout;

namespace Agents.Application.Core;

/// <summary>
/// Abstract base class for all AI agents with Semantic Kernel integration
/// </summary>
public abstract class BaseAgent
{
    protected readonly ILLMProvider _llmProvider;
    protected readonly IPromptLoader _promptLoader;
    protected readonly IEventPublisher _eventPublisher;
    protected readonly ILogger _logger;
    protected readonly IInputSanitizer _inputSanitizer;
    protected readonly string _agentName;
    private readonly ResiliencePipeline _llmResiliencePipeline;

    protected BaseAgent(
        ILLMProvider llmProvider,
        IPromptLoader promptLoader,
        IEventPublisher eventPublisher,
        ILogger logger,
        IInputSanitizer inputSanitizer,
        string agentName)
    {
        _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
        _promptLoader = promptLoader ?? throw new ArgumentNullException(nameof(promptLoader));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _inputSanitizer = inputSanitizer ?? throw new ArgumentNullException(nameof(inputSanitizer));
        _agentName = agentName ?? throw new ArgumentNullException(nameof(agentName));

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
                    _logger.LogWarning(
                        args.Outcome.Exception,
                        "LLM call retry {RetryCount} for agent {AgentName} after {Delay}ms",
                        args.AttemptNumber,
                        _agentName,
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
            _logger.LogInformation(
                "Agent {AgentName} starting execution. ExecutionId: {ExecutionId}, CorrelationId: {CorrelationId}",
                _agentName, context.ExecutionId, context.CorrelationId);

            var result = await ExecuteCoreAsync(input, context);

            stopwatch.Stop();
            // Update duration in metadata since AgentResult is not a record
            result.Metadata["Duration"] = stopwatch.Elapsed;

            _logger.LogInformation(
                "Agent {AgentName} completed execution in {Duration}ms. Success: {IsSuccess}",
                _agentName, stopwatch.ElapsedMilliseconds, result.IsSuccess);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Enhanced logging with comprehensive context
            _logger.LogError(ex,
                "Agent {AgentName} failed. ExecutionId: {ExecutionId}, CorrelationId: {CorrelationId}, " +
                "InitiatedBy: {InitiatedBy}, Duration: {Duration}ms, Input: {InputPreview}",
                _agentName,
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
                    ["AgentName"] = _agentName
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
        var prompt = await _promptLoader.LoadPromptAsync(promptName);
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
        var kernel = _llmProvider.GetKernel();

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
        await _eventPublisher.PublishAsync(domainEvent, cancellationToken);

        _logger.LogInformation(
            "Agent {AgentName} published event {EventType}",
            _agentName, domainEvent.GetType().Name);
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
            var sanitizedValue = _inputSanitizer.Sanitize(rawValue);

            // Log warning if injection patterns were detected
            if (_inputSanitizer.ContainsInjectionPatterns(rawValue))
            {
                _logger.LogWarning(
                    "Potential injection patterns detected in prompt variable '{Key}'. Input has been sanitized. " +
                    "Agent: {AgentName}",
                    key, _agentName);
            }

            result = result.Replace($"{{{{{key}}}}}", sanitizedValue);
        }
        return result;
    }
}
