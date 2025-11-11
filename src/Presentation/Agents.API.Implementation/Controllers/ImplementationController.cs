using Agents.Application.Core;
using Agents.Application.Implementation;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Agents.API.Implementation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImplementationController : ControllerBase
{
    private readonly ImplementationAgent _agent;

    public ImplementationController(ImplementationAgent agent)
    {
        _agent = agent;
    }

    [HttpPost("execute")]
    public async Task<IActionResult> Execute([FromBody] ImplementationRequest request)
    {
        var input = JsonSerializer.Serialize(request);
        var context = new AgentContext { InitiatedBy = User.Identity?.Name ?? "Anonymous" };
        var result = await _agent.ExecuteAsync(input, context);
        return result.IsSuccess ? Ok(result) : StatusCode(500, result);
    }

    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "healthy", service = "implementation-agent" });
}
