using Agents.Application.Core;
using Agents.Application.Notification;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Agents.API.Notification.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class NotificationController : ControllerBase
{
    private readonly NotificationAgent _agent;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(NotificationAgent agent, ILogger<NotificationController> logger)
    {
        _agent = agent;
        _logger = logger;
    }

    /// <summary>
    /// Sends a notification through the specified channel
    /// </summary>
    /// <param name="request">Notification request details</param>
    /// <returns>Result of notification operation</returns>
    [HttpPost("send")]
    [ProducesResponseType(typeof(AgentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SendNotification([FromBody] NotificationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var input = JsonSerializer.Serialize(request);
        var context = new AgentContext
        {
            InitiatedBy = User.Identity?.Name ?? "Anonymous"
        };

        var result = await _agent.ExecuteAsync(input, context);

        if (result.IsSuccess)
        {
            return Ok(result);
        }

        return StatusCode(500, result);
    }

    /// <summary>
    /// Gets the health status of the notification service
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", service = "notification-agent" });
    }
}
