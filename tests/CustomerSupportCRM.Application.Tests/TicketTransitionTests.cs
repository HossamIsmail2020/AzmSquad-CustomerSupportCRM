using CustomerSupportCRM.Domain.Entities;
using CustomerSupportCRM.Domain.Enums;
using Xunit;

namespace CustomerSupportCRM.Application.Tests;

public class TicketTransitionTests
{
    [Theory]
    [InlineData(TicketStatus.Open, TicketStatus.InProgress, true)]
    [InlineData(TicketStatus.Open, TicketStatus.Waiting, true)]
    [InlineData(TicketStatus.Open, TicketStatus.Closed, true)]
    [InlineData(TicketStatus.Open, TicketStatus.Resolved, false)]
    [InlineData(TicketStatus.InProgress, TicketStatus.Resolved, true)]
    [InlineData(TicketStatus.InProgress, TicketStatus.Open, false)]
    [InlineData(TicketStatus.Resolved, TicketStatus.Closed, true)]
    [InlineData(TicketStatus.Resolved, TicketStatus.Open, true)]
    [InlineData(TicketStatus.Resolved, TicketStatus.InProgress, false)]
    [InlineData(TicketStatus.Closed, TicketStatus.Open, true)]
    [InlineData(TicketStatus.Closed, TicketStatus.InProgress, false)]
    [InlineData(TicketStatus.Closed, TicketStatus.Resolved, false)]
    public void IsValidTransition_matches_the_documented_workflow(TicketStatus from, TicketStatus to, bool expected)
    {
        Assert.Equal(expected, Ticket.IsValidTransition(from, to));
    }

    [Fact]
    public void TransitionTo_throws_on_an_invalid_transition()
    {
        var ticket = new Ticket { Status = TicketStatus.Closed };

        var ex = Assert.Throws<InvalidOperationException>(() => ticket.TransitionTo(TicketStatus.Resolved));
        Assert.Contains("Closed", ex.Message);
        Assert.Contains("Resolved", ex.Message);
    }

    [Fact]
    public void TransitionTo_updates_status_and_timestamp_on_a_valid_transition()
    {
        var ticket = new Ticket { Status = TicketStatus.Open, UpdatedAt = DateTime.MinValue };

        ticket.TransitionTo(TicketStatus.InProgress);

        Assert.Equal(TicketStatus.InProgress, ticket.Status);
        Assert.True(ticket.UpdatedAt > DateTime.MinValue);
    }

    [Fact]
    public void Admin_can_assign_a_ticket_to_any_agent_regardless_of_current_assignment()
    {
        var ticket = new Ticket { AssignedAgentId = "agent-1" };

        Assert.True(ticket.CanBeAssignedTo("agent-2", UserRole.Admin, "admin-1"));
    }

    [Fact]
    public void Agent_can_self_assign_an_unassigned_ticket()
    {
        var ticket = new Ticket { AssignedAgentId = null };

        Assert.True(ticket.CanBeAssignedTo("agent-1", UserRole.Agent, "agent-1"));
    }

    [Fact]
    public void Agent_cannot_assign_an_unassigned_ticket_to_someone_else()
    {
        var ticket = new Ticket { AssignedAgentId = null };

        Assert.False(ticket.CanBeAssignedTo("agent-2", UserRole.Agent, "agent-1"));
    }

    [Fact]
    public void Agent_cannot_reassign_a_ticket_already_assigned_to_another_agent()
    {
        var ticket = new Ticket { AssignedAgentId = "agent-2" };

        Assert.False(ticket.CanBeAssignedTo("agent-1", UserRole.Agent, "agent-1"));
    }
}
