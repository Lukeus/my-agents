using Agents.Application.Core;
using Agents.Application.DevOps;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Agents.API.DevOps.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DevOpsController : ControllerBase
{
    private readonly DevOpsAgent _agent;

    public DevOpsController(DevOpsAgent agent)
    {
        _agent = agent;
    }

    [HttpPost("execute")]
    public async Task<IActionResult> Execute([FromBody] DevOpsRequest request)
    {
        var input = JsonSerializer.Serialize(request);
        var context = new AgentContext { InitiatedBy = User.Identity?.Name ?? "Anonymous" };
        var result = await _agent.ExecuteAsync(input, context);
        return result.IsSuccess ? Ok(result) : StatusCode(500, result);
    }

    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "healthy", service = "devops-agent" });
}
