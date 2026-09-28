using CustomerSupportCRM.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupportCRM.Api.Controllers;

[Route("api/auth")]
public class AuthController(IAuthService authService) : ApiControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await authService.LoginAsync(request, ct);
        return result.IsSuccess ? Ok(result.Value) : FromError(result.Error!);
    }

    /// <summary>Admin-only: creates an Agent account. There is no public self-registration — this is a support desk, not a consumer app.</summary>
    [HttpPost("register-agent")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> RegisterAgent(RegisterAgentRequest request, CancellationToken ct)
    {
        var result = await authService.RegisterAgentAsync(request, ct);
        return result.IsSuccess ? NoContent() : FromError(result.Error!);
    }
}
