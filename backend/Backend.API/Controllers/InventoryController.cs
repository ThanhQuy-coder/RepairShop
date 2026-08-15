using RepairShop.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RepairShop.API.Controllers;

[ApiController]
[Route("api/inventory")]
public class InventoryController : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.InventoryViewers)] // FR-045
    public IActionResult GetInventory() => Ok();

    [HttpPost("transactions")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)] // FR-041/042: chỉ Admin nhập/xuất kho thủ công
    public IActionResult CreateTransaction() => Ok();
}