using CustomerSupportCRM.Domain.Enums;

namespace CustomerSupportCRM.Application.Tickets;

public record TicketDto(
    Guid Id,
    string Subject,
    string Description,
    Guid CustomerId,
    string CustomerName,
    string CustomerEmail,
    TicketCategory Category,
    TicketPriority Priority,
    TicketStatus Status,
    string? AssignedAgentId,
    string? AssignedAgentName,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CreateTicketRequest(
    string Subject,
    string Description,
    Guid CustomerId,
    TicketCategory Category,
    TicketPriority Priority);

public record UpdateTicketRequest(
    string Subject,
    string Description,
    TicketCategory Category,
    TicketPriority Priority);

public record ChangeTicketStatusRequest(TicketStatus NewStatus);

public record AssignTicketRequest(string AgentId);

public record TicketActivityDto(
    Guid Id,
    string Type,
    string Description,
    string CreatedByUserId,
    string CreatedByName,
    DateTime CreatedAt);
