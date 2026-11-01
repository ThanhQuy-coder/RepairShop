using RepairShop.Application.Modules.Warranty.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepairShop.Infrastructure.Identity;

namespace RepairShop.API.Controllers;

[ApiController]
[Route("api/warranty")]
public class WarrantyController : ControllerBase
{
    private readonly IMediator _mediator;
    public WarrantyController(IMediator mediator) => _mediator = mediator;

    [HttpGet("my")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> GetMyWarranties()
    {
        var result = await _mediator.Send(new GetMyWarrantiesQuery());
        return Ok(result);
    }

    [HttpGet("staff/search")]
    [Authorize(Policy = AuthorizationPolicies.ReceptionistOrAdmin)]
    public async Task<IActionResult> SearchForStaff(
        [FromQuery] string? phone, [FromQuery] string? warrantyCode, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new SearchWarrantyForStaffQuery(phone, warrantyCode), cancellationToken));
}