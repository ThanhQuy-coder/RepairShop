using RepairShop.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using RepairShop.Application.Modules.Users.Queries;

namespace RepairShop.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)] // áp cho TOÀN BỘ controller
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] string? role,
    [FromQuery] bool? isActive,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 50)
    {
        var result = await _mediator.Send(new GetUsersQuery(role, isActive, page, pageSize));
        return Ok(result);
    }

    [HttpPost]
    public IActionResult CreateUser() => Ok();

    [HttpPatch("{id}/status")]
    public IActionResult ToggleStatus(Guid id) => Ok();
}