using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepairShop.Domain.Common;

[ApiController]
[Route("api/quotes")]
public class QuotesController : ControllerBase
{
    private readonly IMediator _mediator;
    public QuotesController(IMediator mediator) => _mediator = mediator;

    public record RespondQuoteBody(string Decision, string? RejectReason);

    [HttpPatch("{id:guid}/respond")]
    [Authorize(Roles = Roles.Customer)] // FR-032/033: chỉ Customer phản hồi
    public async Task<IActionResult> Respond(Guid id, RespondQuoteBody body)
    {
        var result = await _mediator.Send(new RespondQuoteCommand(id, body.Decision, body.RejectReason));
        return Ok(result);
    }
}