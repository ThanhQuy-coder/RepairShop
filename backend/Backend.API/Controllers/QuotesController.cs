using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepairShop.Application.Modules.Quotes.Commands;
using RepairShop.Domain.Common;

[ApiController]
[Route("api/quotes")]
public class QuotesController : ControllerBase
{
    private readonly IMediator _mediator;
    public QuotesController(IMediator mediator) => _mediator = mediator;

    public record RespondQuoteBody(string Decision, string? RejectReason);

    [HttpPatch("{id:guid}/approve")]
    [Authorize(Roles = Roles.Customer)] // FR-032
    public async Task<IActionResult> Approve(Guid id)
    {
        var result = await _mediator.Send(new ApproveQuoteCommand(id));
        return Ok(result);
    }

    public record RejectBody(string RejectReason);

    [HttpPatch("{id:guid}/reject")]
    [Authorize(Roles = Roles.Customer)] // FR-033
    public async Task<IActionResult> Reject(Guid id, RejectBody body)
    {
        var result = await _mediator.Send(new RejectQuoteCommand(id, body.RejectReason));
        return Ok(result);
    }
}