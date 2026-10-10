using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepairShop.Application.Modules.SLA.Commands;
using RepairShop.Application.Modules.SLA.DTOs;
using RepairShop.Application.Modules.SLA.Queries;
using RepairShop.Domain.Common.Enums;
using RepairShop.Infrastructure.Identity;

namespace RepairShop.API.Controllers;

[ApiController]
[Route("api/sla")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class SlaController(IMediator mediator) : ControllerBase
{
    [HttpGet("policies")]
    public async Task<ActionResult<IReadOnlyList<SlaPolicyResponse>>> GetPolicies(
        CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetSlaPoliciesQuery(), cancellationToken));

    [HttpPost("policies")]
    public async Task<ActionResult<SlaPolicyResponse>> CreatePolicy(
        CreateSlaPolicyRequest request, CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new CreateSlaPolicyCommand(
            request.StatusCode, request.DeviceType, request.DurationMinutes), cancellationToken);
        return CreatedAtAction(nameof(GetPolicies), response);
    }

    [HttpPut("policies/{id:guid}")]
    public async Task<IActionResult> UpdatePolicy(
        Guid id, UpdateSlaPolicyRequest request, CancellationToken cancellationToken)
    {
        await mediator.Send(new UpdateSlaPolicyCommand(
            id, request.DurationMinutes, request.IsActive), cancellationToken);
        return NoContent();
    }

    public sealed record CreateSlaPolicyRequest(
        string StatusCode, DeviceType? DeviceType, int DurationMinutes);

    public sealed record UpdateSlaPolicyRequest(int DurationMinutes, bool IsActive);
}
