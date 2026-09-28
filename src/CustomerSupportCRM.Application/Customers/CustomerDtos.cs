namespace CustomerSupportCRM.Application.Customers;

public record CustomerDto(
    Guid Id,
    string FullName,
    string Email,
    string? Phone,
    string? Company,
    string? Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int TicketCount);

public record CreateCustomerRequest(string FullName, string Email, string? Phone, string? Company, string? Notes);

public record UpdateCustomerRequest(string FullName, string Email, string? Phone, string? Company, string? Notes);
