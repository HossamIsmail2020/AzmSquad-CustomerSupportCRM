using CustomerSupportCRM.Application.Common;
using CustomerSupportCRM.Application.Dashboard;
using CustomerSupportCRM.Application.Tickets;

namespace CustomerSupportCRM.Infrastructure.Services;

public class DashboardService(ITicketService ticketService, ICurrentUserService currentUser) : IDashboardService
{
    public async Task<AgentDashboardDto> GetForCurrentAgentAsync(CancellationToken ct = default)
    {
        var tickets = await ticketService.GetAssignedToAsync(currentUser.UserId, ct);

        var counts = tickets
            .GroupBy(t => t.Status.ToString())
            .Select(g => new DashboardStatusCount(g.Key, g.Count()))
            .OrderBy(c => c.Status)
            .ToList();

        return new AgentDashboardDto(
            currentUser.UserId,
            currentUser.FullName,
            tickets.Count,
            counts,
            tickets);
    }
}
