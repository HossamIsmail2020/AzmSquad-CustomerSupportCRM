using CustomerSupportCRM.Domain.Enums;

namespace CustomerSupportCRM.Domain.Entities;

public class Ticket
{
    private static readonly Dictionary<TicketStatus, TicketStatus[]> AllowedTransitions = new()
    {
        [TicketStatus.Open] = [TicketStatus.InProgress, TicketStatus.Waiting, TicketStatus.Closed],
        [TicketStatus.InProgress] = [TicketStatus.Waiting, TicketStatus.Resolved, TicketStatus.Closed],
        [TicketStatus.Waiting] = [TicketStatus.InProgress, TicketStatus.Resolved, TicketStatus.Closed],
        [TicketStatus.Resolved] = [TicketStatus.Closed, TicketStatus.Open],
        [TicketStatus.Closed] = [TicketStatus.Open]
    };

    public Guid Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public TicketCategory Category { get; set; }
    public TicketPriority Priority { get; set; }
    public TicketStatus Status { get; set; }
    public string? AssignedAgentId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<TicketActivity> Activities { get; set; } = new List<TicketActivity>();

    public static bool IsValidTransition(TicketStatus from, TicketStatus to) =>
        from == to || (AllowedTransitions.TryGetValue(from, out var next) && next.Contains(to));

    /// <summary>Reopen (Resolved/Closed -&gt; Open) is the only backward move; everything else is terminal-forward.</summary>
    public void TransitionTo(TicketStatus newStatus)
    {
        if (!IsValidTransition(Status, newStatus))
        {
            throw new InvalidOperationException($"Cannot transition ticket from '{Status}' to '{newStatus}'.");
        }

        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Admins may assign any ticket to any agent; an Agent may only self-assign a ticket that is currently unassigned.</summary>
    public bool CanBeAssignedTo(string targetAgentId, UserRole actingUserRole, string actingUserId)
    {
        if (actingUserRole == UserRole.Admin)
        {
            return true;
        }

        return targetAgentId == actingUserId && AssignedAgentId is null;
    }
}
