using System.Security.Claims;
using CustomerSupportCRM.Application.Common;
using CustomerSupportCRM.Domain.Enums;

namespace CustomerSupportCRM.Api.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public string UserId => User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("No authenticated user in the current request.");

    public string FullName => User?.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

    public UserRole Role => Enum.Parse<UserRole>(
        User?.FindFirstValue(ClaimTypes.Role)
        ?? throw new InvalidOperationException("No authenticated user in the current request."));
}
