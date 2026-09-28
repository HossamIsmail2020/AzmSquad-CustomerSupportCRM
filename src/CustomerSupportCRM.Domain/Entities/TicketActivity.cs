using CustomerSupportCRM.Domain.Enums;

namespace CustomerSupportCRM.Domain.Entities;

public class TicketActivity
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public Ticket? Ticket { get; set; }
    public TicketActivityType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
