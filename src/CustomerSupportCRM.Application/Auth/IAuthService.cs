using CustomerSupportCRM.Application.Common;

namespace CustomerSupportCRM.Application.Auth;

public interface IAuthService
{
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<Result<bool>> RegisterAgentAsync(RegisterAgentRequest request, CancellationToken ct = default);
}
