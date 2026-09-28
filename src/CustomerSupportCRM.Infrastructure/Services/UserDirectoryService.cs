using CustomerSupportCRM.Application.Users;
using CustomerSupportCRM.Domain.Enums;
using CustomerSupportCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupportCRM.Infrastructure.Services;

public class UserDirectoryService(AppDbContext db) : IUserDirectoryService
{
    public async Task<IReadOnlyList<AgentSummaryDto>> GetAgentsAsync(CancellationToken ct = default) =>
        await db.Users
            .Where(u => u.Role == UserRole.Agent)
            .OrderBy(u => u.FullName)
            .Select(u => new AgentSummaryDto(u.Id, u.FullName, u.Email!))
            .ToListAsync(ct);
}
