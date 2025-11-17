using System.Text.Json;
using Agents.Application.Core;
using Agents.Application.ServiceDesk;
using Microsoft.AspNetCore.Mvc;

namespace Agents.API.ServiceDesk.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServiceDeskController : ControllerBase
{
    private readonly ServiceDeskAgent _agent;

    public ServiceDeskController(ServiceDeskAgent agent)
    {
        _agent = agent;
    }

    [HttpPost("execute")]
    public async Task<IActionResult> Execute([FromBody] ServiceDeskRequest request)
    {
        var input = JsonSerializer.Serialize(request);
        var context = new AgentContext { InitiatedBy = User.Identity?.Name ?? "Anonymous" };
        var result = await _agent.ExecuteAsync(input, context);
        return result.IsSuccess ? Ok(result) : StatusCode(500, result);
    }

    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "healthy", service = "servicedesk-agent" });
}
