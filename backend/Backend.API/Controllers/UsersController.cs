using RepairShop.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using RepairShop.Application.Modules.Users.Queries;
using RepairShop.Application.Modules.Users.Commands;

namespace RepairShop.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.ReceptionistOrAdmin)]
    public async Task<IActionResult> GetUsers([FromQuery] string? role,
    [FromQuery] bool? isActive,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 50)
    {
        var result = await _mediator.Send(new GetUsersQuery(role, isActive, page, pageSize));
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> CreateUser(CreateUserCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetUsers), result);
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> ToggleStatus(Guid id, [FromBody] SetUserStatusBody body)
    {
        var result = await _mediator.Send(new SetUserStatusCommand(id, body.IsActive));
        return Ok(result);
    }

    public record SetUserStatusBody(bool IsActive);
}