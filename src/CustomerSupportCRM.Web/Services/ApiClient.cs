using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CustomerSupportCRM.Application.Auth;
using CustomerSupportCRM.Application.Customers;
using CustomerSupportCRM.Application.Dashboard;
using CustomerSupportCRM.Application.Tickets;
using CustomerSupportCRM.Application.Users;

namespace CustomerSupportCRM.Web.Services;

/// <summary>Thin typed wrapper over HttpClient. Throws ApiException with the server's error message/field errors on non-success so pages can show them without re-parsing responses.</summary>
public class ApiClient(HttpClient http)
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public Task<LoginResponse> LoginAsync(LoginRequest request) =>
        SendAsync<LoginResponse>(() => http.PostAsJsonAsync("api/auth/login", request, JsonOptions));

    public Task<IReadOnlyList<CustomerDto>> GetCustomersAsync() =>
        SendAsync<IReadOnlyList<CustomerDto>>(() => http.GetAsync("api/customers"));

    public Task<CustomerDto> GetCustomerAsync(Guid id) =>
        SendAsync<CustomerDto>(() => http.GetAsync($"api/customers/{id}"));

    public Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request) =>
        SendAsync<CustomerDto>(() => http.PostAsJsonAsync("api/customers", request, JsonOptions));

    public Task<CustomerDto> UpdateCustomerAsync(Guid id, UpdateCustomerRequest request) =>
        SendAsync<CustomerDto>(() => http.PutAsJsonAsync($"api/customers/{id}", request, JsonOptions));

    public async Task DeleteCustomerAsync(Guid id)
    {
        var response = await http.DeleteAsync($"api/customers/{id}");
        await EnsureSuccessAsync(response);
    }

    public Task<IReadOnlyList<TicketDto>> GetTicketsAsync() =>
        SendAsync<IReadOnlyList<TicketDto>>(() => http.GetAsync("api/tickets"));

    public Task<TicketDto> GetTicketAsync(Guid id) =>
        SendAsync<TicketDto>(() => http.GetAsync($"api/tickets/{id}"));

    public Task<TicketDto> CreateTicketAsync(CreateTicketRequest request) =>
        SendAsync<TicketDto>(() => http.PostAsJsonAsync("api/tickets", request, JsonOptions));

    public Task<TicketDto> UpdateTicketAsync(Guid id, UpdateTicketRequest request) =>
        SendAsync<TicketDto>(() => http.PutAsJsonAsync($"api/tickets/{id}", request, JsonOptions));

    public Task<TicketDto> ChangeTicketStatusAsync(Guid id, ChangeTicketStatusRequest request) =>
        SendAsync<TicketDto>(() => http.PostAsJsonAsync($"api/tickets/{id}/status", request, JsonOptions));

    public Task<TicketDto> AssignTicketAsync(Guid id, AssignTicketRequest request) =>
        SendAsync<TicketDto>(() => http.PostAsJsonAsync($"api/tickets/{id}/assign", request, JsonOptions));

    public Task<IReadOnlyList<TicketActivityDto>> GetTicketActivityAsync(Guid id) =>
        SendAsync<IReadOnlyList<TicketActivityDto>>(() => http.GetAsync($"api/tickets/{id}/activity"));

    public Task<AgentDashboardDto> GetMyDashboardAsync() =>
        SendAsync<AgentDashboardDto>(() => http.GetAsync("api/dashboard/me"));

    public Task<IReadOnlyList<AgentSummaryDto>> GetAgentsAsync() =>
        SendAsync<IReadOnlyList<AgentSummaryDto>>(() => http.GetAsync("api/users/agents"));

    private async Task<T> SendAsync<T>(Func<Task<HttpResponseMessage>> send)
    {
        var response = await send();
        await EnsureSuccessAsync(response);
        var result = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        return result!;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        string message = $"Request failed with status {(int)response.StatusCode}.";
        Dictionary<string, string[]>? fieldErrors = null;

        try
        {
            var body = await response.Content.ReadFromJsonAsync<ApiErrorBody>(JsonOptions);
            if (body is not null)
            {
                message = body.Message ?? message;
                fieldErrors = body.Errors;
            }
        }
        catch
        {
            // Body wasn't JSON (e.g. a raw 401) -- fall back to the generic message above.
        }

        throw new ApiException(response.StatusCode, message, fieldErrors);
    }

    private record ApiErrorBody(string? Message, Dictionary<string, string[]>? Errors);
}

public class ApiException(System.Net.HttpStatusCode statusCode, string message, IReadOnlyDictionary<string, string[]>? fieldErrors)
    : Exception(message)
{
    public System.Net.HttpStatusCode StatusCode { get; } = statusCode;
    public IReadOnlyDictionary<string, string[]>? FieldErrors { get; } = fieldErrors;
}
