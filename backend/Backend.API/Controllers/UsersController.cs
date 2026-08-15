using RepairShop.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RepairShop.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)] // áp cho TOÀN BỘ controller
public class UsersController : ControllerBase
{
    [HttpGet]
    public IActionResult GetUsers() => Ok();

    [HttpPost]
    public IActionResult CreateUser() => Ok();

    [HttpPatch("{id}/status")]
    public IActionResult ToggleStatus(Guid id) => Ok();
}