using RepairShop.Application.Modules.Inventory.Commands;
using RepairShop.Application.Modules.Inventory.Queries;
using RepairShop.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RepairShop.API.Controllers;

[ApiController]
[Route("api/inventory")]
public class InventoryController : ControllerBase
{
    private readonly IMediator _mediator;
    public InventoryController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.InventoryViewers)] // FR-045
    public async Task<IActionResult> GetInventory()
    {
        var result = await _mediator.Send(new GetInventoryQuery());
        return Ok(result);
    }

    public record CreateTransactionBody(Guid PartId, string Type, int Quantity);

    [HttpPost("transactions")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)] // FR-041/042
    public async Task<IActionResult> CreateTransaction(CreateTransactionBody body)
    {
        var result = await _mediator.Send(new CreateInventoryTransactionCommand(body.PartId, body.Type, body.Quantity));
        return Ok(result);
    }

    [HttpGet("transactions")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] Guid? partId, [FromQuery] string? type,
        [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetInventoryTransactionsQuery(partId, type, fromDate, toDate, page, pageSize));
        return Ok(result);
    }

    [HttpGet("dashboard")]
    [Authorize(Policy = AuthorizationPolicies.InventoryViewers)]
    public async Task<IActionResult> GetDashboard()
    {
        var result = await _mediator.Send(new GetInventoryDashboardQuery());
        return Ok(result);
    }

    [HttpGet("low-stock")]
    [Authorize(Policy = AuthorizationPolicies.InventoryViewers)]
    public async Task<IActionResult> GetLowStock([FromQuery] string filter = "All")
    {
        var stockFilter = Enum.TryParse<StockFilter>(filter, true, out var f)
            ? f : StockFilter.All;

        var result = await _mediator.Send(new GetLowStockPartsQuery(stockFilter));
        return Ok(result);
    }
}