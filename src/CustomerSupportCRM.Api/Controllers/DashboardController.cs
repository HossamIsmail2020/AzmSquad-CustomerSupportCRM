using CustomerSupportCRM.Application.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupportCRM.Api.Controllers;

[Route("api/dashboard")]
[Authorize]
public class DashboardController(IDashboardService dashboardService) : ApiControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<AgentDashboardDto>> GetMine(CancellationToken ct) =>
        Ok(await dashboardService.GetForCurrentAgentAsync(ct));
}
