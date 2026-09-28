using CustomerSupportCRM.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupportCRM.Api.Controllers;

[Route("api/users")]
[Authorize]
public class UsersController(IUserDirectoryService userDirectory) : ApiControllerBase
{
    [HttpGet("agents")]
    public async Task<ActionResult<IReadOnlyList<AgentSummaryDto>>> GetAgents(CancellationToken ct) =>
        Ok(await userDirectory.GetAgentsAsync(ct));
}
