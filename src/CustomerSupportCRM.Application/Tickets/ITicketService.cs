using CustomerSupportCRM.Application.Common;

namespace CustomerSupportCRM.Application.Tickets;

public interface ITicketService
{
    Task<IReadOnlyList<TicketDto>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TicketDto>> GetAssignedToAsync(string agentId, CancellationToken ct = default);
    Task<Result<TicketDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<TicketDto>> CreateAsync(CreateTicketRequest request, CancellationToken ct = default);
    Task<Result<TicketDto>> UpdateAsync(Guid id, UpdateTicketRequest request, CancellationToken ct = default);
    Task<Result<TicketDto>> ChangeStatusAsync(Guid id, ChangeTicketStatusRequest request, CancellationToken ct = default);
    Task<Result<TicketDto>> AssignAsync(Guid id, AssignTicketRequest request, CancellationToken ct = default);
    Task<Result<IReadOnlyList<TicketActivityDto>>> GetActivityAsync(Guid id, CancellationToken ct = default);
}
