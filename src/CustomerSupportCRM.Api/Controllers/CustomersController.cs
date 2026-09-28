using CustomerSupportCRM.Application.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupportCRM.Api.Controllers;

[Route("api/customers")]
[Authorize]
public class CustomersController(ICustomerService customerService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CustomerDto>>> GetAll(CancellationToken ct) =>
        Ok(await customerService.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await customerService.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : FromError(result.Error!);
    }

    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create(CreateCustomerRequest request, CancellationToken ct)
    {
        var result = await customerService.CreateAsync(request, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : FromError(result.Error!);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CustomerDto>> Update(Guid id, UpdateCustomerRequest request, CancellationToken ct)
    {
        var result = await customerService.UpdateAsync(id, request, ct);
        return result.IsSuccess ? Ok(result.Value) : FromError(result.Error!);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await customerService.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : FromError(result.Error!);
    }
}
