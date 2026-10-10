using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepairShop.Application.Modules.SLA.DTOs;
using RepairShop.Application.Modules.SLA.Queries;
using RepairShop.Infrastructure.Identity;

namespace RepairShop.API.Controllers;

[ApiController]
[Route("api/reports/sla-summary")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class SlaReportsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<SlaSummaryResponse>> Get(CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetSlaSummaryQuery(), cancellationToken));
}
