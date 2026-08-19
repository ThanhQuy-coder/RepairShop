using RepairShop.Application.Modules.Tickets.Commands;
using RepairShop.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RepairShop.API.Controllers;

[ApiController]
[Route("api/tickets")]
public class TicketsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TicketsController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ReceptionistOrAdmin)] // FR-015: Receptionist tạo ticket
    public async Task<IActionResult> Create(CreateTicketCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(Create), new { id = result.Id }, result);
        // Lưu ý: chưa có GET /api/tickets/{id} thật (thuộc Task khác trong Tuần 4) nên tạm route lại chính action này —
        // sẽ đổi thành nameof(GetById) khi task đó hoàn thành.
    }
}