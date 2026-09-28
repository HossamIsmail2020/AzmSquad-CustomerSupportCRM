namespace CustomerSupportCRM.Application.Dashboard;

public interface IDashboardService
{
    Task<AgentDashboardDto> GetForCurrentAgentAsync(CancellationToken ct = default);
}
