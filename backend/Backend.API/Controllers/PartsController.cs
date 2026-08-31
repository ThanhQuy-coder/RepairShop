using RepairShop.Application.Modules.Inventory.Queries;
using RepairShop.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RepairShop.API.Controllers;

[ApiController]
[Route("api/parts")]
[Authorize(Policy = AuthorizationPolicies.InventoryViewers)] // Technician + Admin (Task 7, Tuần 3)
public class PartsController : ControllerBase
{
    private readonly IMediator _mediator;
    public PartsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetParts([FromQuery] string? search) =>
        Ok(await _mediator.Send(new GetPartsQuery(search)));
}