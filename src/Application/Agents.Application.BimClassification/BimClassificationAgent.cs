using Agents.Application.Core;
using Agents.Application.BimClassification.Requests;
using Agents.Application.BimClassification.Responses;
using Agents.Domain.BimClassification.Entities;
using Agents.Domain.Core.Interfaces;
using Agents.Infrastructure.Prompts.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Agents.Application.BimClassification;

/// <summary>
/// Agent responsible for analyzing BIM elements and proposing classifications.
/// IMPORTANT: This agent produces SUGGESTIONS only, never direct classifications.
/// </summary>
public class BimClassificationAgent : BaseAgent
{
    public BimClassificationAgent(
        ILLMProvider llmProvider,
        IPromptLoader promptLoader,
        IEventPublisher eventPublisher,
        ILogger<BimClassificationAgent> logger)
        : base(llmProvider, promptLoader, eventPublisher, logger, "BimClassificationAgent")
    {
    }

    protected override async Task<AgentResult> ExecuteCoreAsync(
        string input,
        AgentContext context)
    {
        try
        {
            var request = JsonSerializer.Deserialize<ClassifyBimElementRequest>(input);
            if (request == null)
            {
                return AgentResult.Failure("Invalid BIM classification request format");
            }

            // Load system prompt (contains safety constraints)
            var systemPrompt = await LoadPromptAsync(
                "prompts/bim-classifier/system.prompt",
                new Dictionary<string, object>());

            // Load user prompt with element data
            var userPrompt = await LoadPromptAsync(
                "prompts/bim-classifier/user.prompt",
                new Dictionary<string, object>
                {
                    ["elementJson"] = request.ElementJson ?? "{}",
                    ["existingClassificationJson"] = request.ExistingClassificationJson ?? "{}"
                });

            var fullPrompt = $"{systemPrompt}\n\n{userPrompt}";

            // Invoke LLM
            var rawResult = await InvokeKernelAsync(
                fullPrompt,
                cancellationToken: context.CancellationToken);

            // Parse and normalize to suggestion JSON
            var normalizedJson = NormalizeToSuggestionJson(rawResult);

            // Deserialize to validate structure
            var suggestion = JsonSerializer.Deserialize<BimClassificationSuggestion>(
                normalizedJson);

            if (suggestion == null)
            {
                return AgentResult.Failure("Failed to parse LLM output into valid suggestion");
            }

            // Publish event
            await PublishEventAsync(
                suggestion.DomainEvents.First(),
                context.CancellationToken);

            return AgentResult<ClassifyBimElementResponse>.Success(
                new ClassifyBimElementResponse
                {
                    SuggestionId = suggestion.Id,
                    RawModelOutput = rawResult,
                    NormalizedSuggestionJson = normalizedJson
                },
                "Classification suggestion generated successfully",
                new Dictionary<string, object>
                {
                    ["BimElementId"] = request.BimElementId,
                    ["SuggestionId"] = suggestion.Id
                });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error classifying BIM element");
            return AgentResult.Failure($"Error: {ex.Message}");
        }
    }

    private static string NormalizeToSuggestionJson(string raw)
    {
        // V1: Simple trim and validation
        // V2: Add JSON repair logic if needed
        var trimmed = raw.Trim();

        // Basic validation: must start with { and end with }
        if (!trimmed.StartsWith("{") || !trimmed.EndsWith("}"))
        {
            throw new InvalidOperationException(
                "LLM output is not valid JSON. Output must be strict JSON.");
        }

        return trimmed;
    }
}
