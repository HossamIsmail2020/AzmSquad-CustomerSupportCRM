using CustomerSupportCRM.Application.Tickets;

namespace CustomerSupportCRM.Application.Dashboard;

public record DashboardStatusCount(string Status, int Count);

public record AgentDashboardDto(
    string AgentId,
    string AgentName,
    int TotalAssigned,
    IReadOnlyList<DashboardStatusCount> CountsByStatus,
    IReadOnlyList<TicketDto> AssignedTickets);
