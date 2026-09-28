namespace CustomerSupportCRM.Application.Users;

/// <summary>Read-only agent directory so ticket assignment UIs can offer a picker. Deliberately minimal -- no full user management, which is bonus (Security & Administration) scope.</summary>
public interface IUserDirectoryService
{
    Task<IReadOnlyList<AgentSummaryDto>> GetAgentsAsync(CancellationToken ct = default);
}
