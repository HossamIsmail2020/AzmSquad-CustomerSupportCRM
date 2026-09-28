using CustomerSupportCRM.Domain.Enums;

namespace CustomerSupportCRM.Application.Auth;

public record LoginRequest(string Email, string Password);

public record LoginResponse(string Token, DateTimeOffset ExpiresAt, string UserId, string FullName, UserRole Role);

public record RegisterAgentRequest(string FullName, string Email, string Password);
