using CustomerSupportCRM.Application.Auth;
using CustomerSupportCRM.Application.Common;
using CustomerSupportCRM.Domain.Enums;
using CustomerSupportCRM.Infrastructure.Auth;
using CustomerSupportCRM.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace CustomerSupportCRM.Infrastructure.Services;

public class AuthService(
    UserManager<ApplicationUser> userManager,
    JwtTokenGenerator tokenGenerator) : IAuthService
{
    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            return Result<LoginResponse>.Failure(ServiceError.Validation("Invalid email or password."));
        }

        var (token, expiresAt) = tokenGenerator.Generate(user);
        return Result<LoginResponse>.Success(new LoginResponse(token, expiresAt, user.Id, user.FullName, user.Role));
    }

    public async Task<Result<bool>> RegisterAgentAsync(RegisterAgentRequest request, CancellationToken ct = default)
    {
        if (await userManager.FindByEmailAsync(request.Email) is not null)
        {
            return Result<bool>.Failure(ServiceError.Conflict($"A user with email '{request.Email}' already exists."));
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            Role = UserRole.Agent,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var fields = new Dictionary<string, string[]>
            {
                ["password"] = result.Errors.Select(e => e.Description).ToArray()
            };
            return Result<bool>.Failure(ServiceError.Validation("Could not create agent account.", fields));
        }

        return Result<bool>.Success(true);
    }
}
