using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CustomerSupportCRM.Application.Auth;
using CustomerSupportCRM.Application.Customers;
using CustomerSupportCRM.Domain.Enums;
using Xunit;
using static CustomerSupportCRM.Api.Tests.CustomWebApplicationFactory;

namespace CustomerSupportCRM.Api.Tests;

public class CustomerApiTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory = new();
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _factory.InitializeDatabaseAsync();
        await _factory.CreateUserAsync("admin@test.dev", "Passw0rd!", "Test Admin", UserRole.Admin);

        _client = _factory.CreateClient();
        var login = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin@test.dev", "Passw0rd!"), JsonOptions);
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return _factory.DisposeAsync().AsTask();
    }

    [Fact]
    public async Task Unauthenticated_request_is_rejected()
    {
        using var anonymousClient = _factory.CreateClient();
        var response = await anonymousClient.GetAsync("/api/customers");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_get_update_delete_round_trip_succeeds()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/customers",
            new CreateCustomerRequest("Jane Doe", "jane.doe@example.com", "+201234567890", "Acme", "VIP"), JsonOptions);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CustomerDto>(JsonOptions);
        Assert.NotNull(created);

        var getResponse = await _client.GetAsync($"/api/customers/{created!.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var updateResponse = await _client.PutAsJsonAsync($"/api/customers/{created.Id}",
            new UpdateCustomerRequest("Jane A. Doe", "jane.doe@example.com", "+201234567890", "Acme", "VIP customer"), JsonOptions);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<CustomerDto>(JsonOptions);
        Assert.Equal("Jane A. Doe", updated!.FullName);

        var deleteResponse = await _client.DeleteAsync($"/api/customers/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getAfterDelete = await _client.GetAsync($"/api/customers/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getAfterDelete.StatusCode);
    }

    [Fact]
    public async Task Creating_a_customer_with_a_duplicate_email_returns_conflict()
    {
        var request = new CreateCustomerRequest("First", "dup@example.com", null, null, null);
        var first = await _client.PostAsJsonAsync("/api/customers", request, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await _client.PostAsJsonAsync("/api/customers",
            new CreateCustomerRequest("Second", "dup@example.com", null, null, null), JsonOptions);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Creating_a_customer_with_invalid_email_returns_bad_request()
    {
        var response = await _client.PostAsJsonAsync("/api/customers",
            new CreateCustomerRequest("No Email", "not-an-email", null, null, null), JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
