using Agents.Application.Core;
using Agents.Domain.Core.Interfaces;
using Agents.Infrastructure.Prompts.Services;
using Agents.Shared.Security;
using Microsoft.Extensions.Logging;

namespace Agents.Application.ServiceDesk;

/// <summary>
/// Agent responsible for service desk ticket triage, solution suggestions, and SLA tracking
/// </summary>
public class ServiceDeskAgent : BaseAgent
{
    public ServiceDeskAgent(
        ILLMProvider llmProvider,
        IPromptLoader promptLoader,
        IEventPublisher eventPublisher,
        IInputSanitizer inputSanitizer,
        ILogger<ServiceDeskAgent> logger)
        : base(llmProvider, promptLoader, eventPublisher, logger, inputSanitizer, "ServiceDeskAgent")
    {
    }

    protected override async Task<AgentResult> ExecuteCoreAsync(string input, AgentContext context)
    {
        try
        {
            var request = System.Text.Json.JsonSerializer.Deserialize<ServiceDeskRequest>(input);
            if (request == null)
            {
                return AgentResult.Failure("Invalid service desk request format");
            }

            return request.Action.ToLowerInvariant() switch
            {
                "triage_ticket" => await TriageTicketAsync(request, context),
                "suggest_solution" => await SuggestSolutionAsync(request, context),
                "check_sla" => await CheckSLAAsync(request, context),
                "escalate" => await EscalateTicketAsync(request, context),
                _ => AgentResult.Failure($"Unknown action: {request.Action}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing service desk request");
            return AgentResult.Failure($"Error: {ex.Message}");
        }
    }

    private async Task<AgentResult> TriageTicketAsync(ServiceDeskRequest request, AgentContext context)
    {
        var promptText = await LoadPromptAsync("prompts/servicedesk-ticket-triager.prompt", new Dictionary<string, object>
        {
            ["ticketTitle"] = request.TicketTitle,
            ["ticketDescription"] = request.TicketDescription,
            ["category"] = request.Category ?? "General"
        });

        var triage = await InvokeKernelAsync(promptText, cancellationToken: context.CancellationToken);

        _logger.LogInformation("Triaged ticket: {TicketId}", request.TicketId);

        return AgentResult<TriageResult>.Success(
            new TriageResult
            {
                TicketId = request.TicketId,
                Priority = "Medium", // Parsed from LLM response
                Category = "Technical",
                AssignedTeam = "Engineering",
                TriageNotes = triage
            },
            "Ticket triaged successfully");
    }

    private async Task<AgentResult> SuggestSolutionAsync(ServiceDeskRequest request, AgentContext context)
    {
        var promptText = await LoadPromptAsync("prompts/servicedesk-solution-suggester.prompt", new Dictionary<string, object>
        {
            ["ticketDescription"] = request.TicketDescription,
            ["knowledgeBase"] = "Access to knowledge base articles" // TODO: Integrate with actual KB
        });

        var solution = await InvokeKernelAsync(promptText, cancellationToken: context.CancellationToken);

        return AgentResult<string>.Success(solution, "Solution suggested");
    }

    private async Task<AgentResult> CheckSLAAsync(ServiceDeskRequest request, AgentContext context)
    {
        // TODO: Calculate actual SLA based on ticket priority and creation time
        var slaStatus = new SLAStatus
        {
            TicketId = request.TicketId,
            Priority = "Medium",
            TimeRemaining = TimeSpan.FromHours(12),
            IsAtRisk = false
        };

        await Task.CompletedTask;

        return AgentResult<SLAStatus>.Success(slaStatus, "SLA status checked");
    }

    private async Task<AgentResult> EscalateTicketAsync(ServiceDeskRequest request, AgentContext context)
    {
        var promptText = await LoadPromptAsync("prompts/servicedesk-escalation-analyzer.prompt", new Dictionary<string, object>
        {
            ["ticketDescription"] = request.TicketDescription,
            ["reason"] = request.Category ?? "SLA breach"
        });

        var escalationNotes = await InvokeKernelAsync(promptText, cancellationToken: context.CancellationToken);

        _logger.LogWarning("Escalating ticket: {TicketId}", request.TicketId);

        return AgentResult.Success($"Ticket escalated: {escalationNotes}");
    }
}

public record ServiceDeskRequest
{
    public required string Action { get; init; }
    public required string TicketId { get; init; }
    public required string TicketTitle { get; init; }
    public required string TicketDescription { get; init; }
    public string? Category { get; init; }
}

public record TriageResult
{
    public required string TicketId { get; init; }
    public required string Priority { get; init; }
    public required string Category { get; init; }
    public required string AssignedTeam { get; init; }
    public required string TriageNotes { get; init; }
}

public record SLAStatus
{
    public required string TicketId { get; init; }
    public required string Priority { get; init; }
    public required TimeSpan TimeRemaining { get; init; }
    public required bool IsAtRisk { get; init; }
}
