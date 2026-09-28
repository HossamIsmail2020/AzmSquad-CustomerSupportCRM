using CustomerSupportCRM.Domain.Enums;

namespace CustomerSupportCRM.Application.Common;

/// <summary>Abstracts the acting user's identity away from HttpContext so Application services stay testable without ASP.NET Core.</summary>
public interface ICurrentUserService
{
    string UserId { get; }
    string FullName { get; }
    UserRole Role { get; }
}
