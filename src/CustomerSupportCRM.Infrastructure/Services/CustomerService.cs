using CustomerSupportCRM.Application.Common;
using CustomerSupportCRM.Application.Customers;
using CustomerSupportCRM.Domain.Entities;
using CustomerSupportCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupportCRM.Infrastructure.Services;

public class CustomerService(AppDbContext db) : ICustomerService
{
    public async Task<IReadOnlyList<CustomerDto>> GetAllAsync(CancellationToken ct = default) =>
        await db.Customers
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CustomerDto(
                c.Id, c.FullName, c.Email, c.Phone, c.Company, c.Notes, c.CreatedAt, c.UpdatedAt,
                c.Tickets.Count))
            .ToListAsync(ct);

    public async Task<Result<CustomerDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var result = await db.Customers
            .Where(c => c.Id == id)
            .Select(c => new CustomerDto(
                c.Id, c.FullName, c.Email, c.Phone, c.Company, c.Notes, c.CreatedAt, c.UpdatedAt,
                c.Tickets.Count))
            .FirstOrDefaultAsync(ct);

        return result is null
            ? Result<CustomerDto>.Failure(ServiceError.NotFound($"Customer '{id}' was not found."))
            : Result<CustomerDto>.Success(result);
    }

    public async Task<Result<CustomerDto>> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default)
    {
        var validationError = CustomerValidator.Validate(request.FullName, request.Email, request.Phone);
        if (validationError is not null)
        {
            return Result<CustomerDto>.Failure(validationError);
        }

        if (await db.Customers.AnyAsync(c => c.Email == request.Email, ct))
        {
            return Result<CustomerDto>.Failure(ServiceError.Conflict($"A customer with email '{request.Email}' already exists."));
        }

        var now = DateTime.UtcNow;
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim(),
            Phone = request.Phone?.Trim(),
            Company = request.Company?.Trim(),
            Notes = request.Notes?.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Customers.Add(customer);
        await db.SaveChangesAsync(ct);
        return Result<CustomerDto>.Success(ToDto(customer, ticketCount: 0));
    }

    public async Task<Result<CustomerDto>> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken ct = default)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (customer is null)
        {
            return Result<CustomerDto>.Failure(ServiceError.NotFound($"Customer '{id}' was not found."));
        }

        var validationError = CustomerValidator.Validate(request.FullName, request.Email, request.Phone);
        if (validationError is not null)
        {
            return Result<CustomerDto>.Failure(validationError);
        }

        if (await db.Customers.AnyAsync(c => c.Email == request.Email && c.Id != id, ct))
        {
            return Result<CustomerDto>.Failure(ServiceError.Conflict($"A customer with email '{request.Email}' already exists."));
        }

        customer.FullName = request.FullName.Trim();
        customer.Email = request.Email.Trim();
        customer.Phone = request.Phone?.Trim();
        customer.Company = request.Company?.Trim();
        customer.Notes = request.Notes?.Trim();
        customer.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        var ticketCount = await db.Tickets.CountAsync(t => t.CustomerId == id, ct);
        return Result<CustomerDto>.Success(ToDto(customer, ticketCount));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (customer is null)
        {
            return Result<bool>.Failure(ServiceError.NotFound($"Customer '{id}' was not found."));
        }

        if (await db.Tickets.AnyAsync(t => t.CustomerId == id, ct))
        {
            return Result<bool>.Failure(ServiceError.Conflict("Cannot delete a customer that has tickets. Close or reassign their tickets first."));
        }

        db.Customers.Remove(customer);
        await db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    private static CustomerDto ToDto(Customer c, int ticketCount) => new(
        c.Id, c.FullName, c.Email, c.Phone, c.Company, c.Notes, c.CreatedAt, c.UpdatedAt,
        ticketCount);
}
