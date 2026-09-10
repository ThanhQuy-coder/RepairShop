using RepairShop.Application.Modules.Inventory.Commands;
using RepairShop.Application.Modules.Inventory.Queries;
using RepairShop.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RepairShop.API.Controllers;

[ApiController]
[Route("api/parts")]
public class PartsController : ControllerBase
{
    private readonly IMediator _mediator;
    public PartsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.InventoryViewers)] // Technician + Admin (Task 7 Tuần 3)
    public async Task<IActionResult> GetParts(
        [FromQuery] string? search, [FromQuery] string? category,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetPartsListQuery(search, category, page, pageSize));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.InventoryViewers)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetPartByIdQuery(id));
        return Ok(result);
    }

    public record CreatePartBody(string Name, string Sku, decimal CostPrice, decimal UnitPrice,
        string? Category, string? CompatibleDeviceType, string Unit, int MinStockThreshold);

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)] // FR-040: chỉ Admin thêm linh kiện mới
    public async Task<IActionResult> Create(CreatePartBody body)
    {
        var result = await _mediator.Send(new CreatePartCommand(
            body.Name, body.Sku, body.CostPrice, body.UnitPrice,
            body.Category, body.CompatibleDeviceType, body.Unit, body.MinStockThreshold));
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    public record UpdatePartBody(string Name, decimal CostPrice, decimal UnitPrice,
        string? Category, string? CompatibleDeviceType, string Unit, int MinStockThreshold);

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Update(Guid id, UpdatePartBody body)
    {
        var result = await _mediator.Send(new UpdatePartCommand(id,
            body.Name, body.CostPrice, body.UnitPrice, body.Category, body.CompatibleDeviceType,
            body.Unit, body.MinStockThreshold));
        return Ok(result);
    }
}