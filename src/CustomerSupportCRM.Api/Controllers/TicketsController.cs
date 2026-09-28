using CustomerSupportCRM.Application.Tickets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupportCRM.Api.Controllers;

[Route("api/tickets")]
[Authorize]
public class TicketsController(ITicketService ticketService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TicketDto>>> GetAll(CancellationToken ct) =>
        Ok(await ticketService.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TicketDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await ticketService.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : FromError(result.Error!);
    }

    [HttpPost]
    public async Task<ActionResult<TicketDto>> Create(CreateTicketRequest request, CancellationToken ct)
    {
        var result = await ticketService.CreateAsync(request, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : FromError(result.Error!);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TicketDto>> Update(Guid id, UpdateTicketRequest request, CancellationToken ct)
    {
        var result = await ticketService.UpdateAsync(id, request, ct);
        return result.IsSuccess ? Ok(result.Value) : FromError(result.Error!);
    }

    [HttpPost("{id:guid}/status")]
    public async Task<ActionResult<TicketDto>> ChangeStatus(Guid id, ChangeTicketStatusRequest request, CancellationToken ct)
    {
        var result = await ticketService.ChangeStatusAsync(id, request, ct);
        return result.IsSuccess ? Ok(result.Value) : FromError(result.Error!);
    }

    [HttpPost("{id:guid}/assign")]
    public async Task<ActionResult<TicketDto>> Assign(Guid id, AssignTicketRequest request, CancellationToken ct)
    {
        var result = await ticketService.AssignAsync(id, request, ct);
        return result.IsSuccess ? Ok(result.Value) : FromError(result.Error!);
    }

    [HttpGet("{id:guid}/activity")]
    public async Task<ActionResult<IReadOnlyList<TicketActivityDto>>> GetActivity(Guid id, CancellationToken ct)
    {
        var result = await ticketService.GetActivityAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : FromError(result.Error!);
    }
}
