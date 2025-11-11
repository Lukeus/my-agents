using Agents.Application.Core;
using Agents.Application.TestPlanning;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Agents.API.TestPlanning.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestPlanningController : ControllerBase
{
    private readonly TestPlanningAgent _agent;

    public TestPlanningController(TestPlanningAgent agent)
    {
        _agent = agent;
    }

    [HttpPost("execute")]
    public async Task<IActionResult> Execute([FromBody] TestPlanningRequest request)
    {
        var input = JsonSerializer.Serialize(request);
        var context = new AgentContext { InitiatedBy = User.Identity?.Name ?? "Anonymous" };
        var result = await _agent.ExecuteAsync(input, context);
        return result.IsSuccess ? Ok(result) : StatusCode(500, result);
    }

    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "healthy", service = "testplanning-agent" });
}
