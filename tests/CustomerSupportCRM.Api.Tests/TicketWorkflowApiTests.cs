using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CustomerSupportCRM.Application.Auth;
using CustomerSupportCRM.Application.Customers;
using CustomerSupportCRM.Application.Dashboard;
using CustomerSupportCRM.Application.Tickets;
using CustomerSupportCRM.Domain.Enums;
using Xunit;
using static CustomerSupportCRM.Api.Tests.CustomWebApplicationFactory;

namespace CustomerSupportCRM.Api.Tests;

public class TicketWorkflowApiTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory = new();
    private HttpClient _adminClient = null!;
    private HttpClient _agentOneClient = null!;
    private HttpClient _agentTwoClient = null!;
    private string _agentOneId = null!;
    private string _agentTwoId = null!;

    public async Task InitializeAsync()
    {
        await _factory.InitializeDatabaseAsync();
        await _factory.CreateUserAsync("admin@test.dev", "Passw0rd!", "Test Admin", UserRole.Admin);
        _agentOneId = await _factory.CreateUserAsync("agent1@test.dev", "Passw0rd!", "Agent One", UserRole.Agent);
        _agentTwoId = await _factory.CreateUserAsync("agent2@test.dev", "Passw0rd!", "Agent Two", UserRole.Agent);

        _adminClient = await AuthenticatedClientAsync("admin@test.dev");
        _agentOneClient = await AuthenticatedClientAsync("agent1@test.dev");
        _agentTwoClient = await AuthenticatedClientAsync("agent2@test.dev");
    }

    public Task DisposeAsync()
    {
        _adminClient.Dispose();
        _agentOneClient.Dispose();
        _agentTwoClient.Dispose();
        return _factory.DisposeAsync().AsTask();
    }

    private async Task<HttpClient> AuthenticatedClientAsync(string email)
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "Passw0rd!"), JsonOptions);
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    private async Task<Guid> CreateCustomerAsync()
    {
        var response = await _adminClient.PostAsJsonAsync("/api/customers",
            new CreateCustomerRequest("Test Customer", $"{Guid.NewGuid()}@example.com", null, null, null), JsonOptions);
        var dto = await response.Content.ReadFromJsonAsync<CustomerDto>(JsonOptions);
        return dto!.Id;
    }

    private async Task<TicketDto> CreateTicketAsync()
    {
        var customerId = await CreateCustomerAsync();
        var response = await _adminClient.PostAsJsonAsync("/api/tickets",
            new CreateTicketRequest("Login issue", "Cannot log in.", customerId, TicketCategory.Account, TicketPriority.High), JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<TicketDto>(JsonOptions))!;
    }

    [Fact]
    public async Task New_ticket_starts_in_Open_status_with_an_activity_entry()
    {
        var ticket = await CreateTicketAsync();
        Assert.Equal(TicketStatus.Open, ticket.Status);

        var activity = await _adminClient.GetFromJsonAsync<List<TicketActivityDto>>($"/api/tickets/{ticket.Id}/activity", JsonOptions);
        Assert.Single(activity!);
        Assert.Equal("Created", activity![0].Type);
    }

    [Fact]
    public async Task Valid_status_transition_succeeds_and_is_logged()
    {
        var ticket = await CreateTicketAsync();

        var response = await _adminClient.PostAsJsonAsync($"/api/tickets/{ticket.Id}/status",
            new ChangeTicketStatusRequest(TicketStatus.InProgress), JsonOptions);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var activity = await _adminClient.GetFromJsonAsync<List<TicketActivityDto>>($"/api/tickets/{ticket.Id}/activity", JsonOptions);
        Assert.Equal(2, activity!.Count);
        Assert.Equal("StatusChanged", activity[1].Type);
    }

    [Fact]
    public async Task Invalid_status_transition_is_rejected_with_conflict()
    {
        var ticket = await CreateTicketAsync();

        // Open -> Resolved is not an allowed direct transition.
        var response = await _adminClient.PostAsJsonAsync($"/api/tickets/{ticket.Id}/status",
            new ChangeTicketStatusRequest(TicketStatus.Resolved), JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Admin_can_assign_a_ticket_to_any_agent()
    {
        var ticket = await CreateTicketAsync();

        var response = await _adminClient.PostAsJsonAsync($"/api/tickets/{ticket.Id}/assign",
            new AssignTicketRequest(_agentOneId), JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<TicketDto>(JsonOptions);
        Assert.Equal(_agentOneId, updated!.AssignedAgentId);
    }

    [Fact]
    public async Task Agent_can_self_assign_an_unassigned_ticket()
    {
        var ticket = await CreateTicketAsync();

        var response = await _agentOneClient.PostAsJsonAsync($"/api/tickets/{ticket.Id}/assign",
            new AssignTicketRequest(_agentOneId), JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Agent_cannot_assign_an_unassigned_ticket_to_a_different_agent()
    {
        var ticket = await CreateTicketAsync();

        var response = await _agentOneClient.PostAsJsonAsync($"/api/tickets/{ticket.Id}/assign",
            new AssignTicketRequest(_agentTwoId), JsonOptions);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Dashboard_only_returns_the_calling_agents_own_tickets()
    {
        var ticketForAgentOne = await CreateTicketAsync();
        await _adminClient.PostAsJsonAsync($"/api/tickets/{ticketForAgentOne.Id}/assign", new AssignTicketRequest(_agentOneId), JsonOptions);

        var ticketForAgentTwo = await CreateTicketAsync();
        await _adminClient.PostAsJsonAsync($"/api/tickets/{ticketForAgentTwo.Id}/assign", new AssignTicketRequest(_agentTwoId), JsonOptions);

        var dashboard = await _agentOneClient.GetFromJsonAsync<AgentDashboardDto>("/api/dashboard/me", JsonOptions);

        Assert.Equal(1, dashboard!.TotalAssigned);
        Assert.Equal(ticketForAgentOne.Id, dashboard.AssignedTickets.Single().Id);
    }
}
