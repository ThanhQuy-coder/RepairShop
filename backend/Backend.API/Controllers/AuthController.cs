using RepairShop.Application.Modules.Identity.Queries;
using Microsoft.AspNetCore.Authorization;
using RepairShop.Application.Modules.Identity.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace RepairShop.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator) => _mediator = mediator;

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var result = await _mediator.Send(new GetProfileQuery());
        return Ok(result);
    }
}