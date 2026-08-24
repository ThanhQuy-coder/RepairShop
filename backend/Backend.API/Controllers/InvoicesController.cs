using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepairShop.Infrastructure.Identity;

[ApiController]
[Route("api/invoices")]
public class InvoicesController : ControllerBase
{
    private readonly IMediator _mediator;
    public InvoicesController(IMediator mediator) => _mediator = mediator;

    public record PayBody(DateTime? PaidAt);

    [HttpPatch("{id:guid}/pay")]
    [Authorize(Policy = AuthorizationPolicies.ReceptionistOrAdmin)]
    public async Task<IActionResult> Pay(Guid id, PayBody body)
    {
        var result = await _mediator.Send(new MarkInvoicePaidCommand(id, body.PaidAt));
        return Ok(result);
    }
}