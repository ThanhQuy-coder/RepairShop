using Backend.Application.Common.Exceptions;
using Backend.Application.Modules.Identity;
using Backend.Application.Modules.Identity.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Backend.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        try
        {
            var result = await _authService.RegisterAsync(request);
            return Ok(result);
        }
        catch (EmailAlreadyExistsException ex)
        {
            return Conflict(new { success = false, errorCode = "EMAIL_EXISTS", message = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        try
        {
            var result = await _authService.LoginAsync(request);
            return Ok(result);
        }
        catch (InvalidCredentialsException ex)
        {
            return Unauthorized(new { success = false, errorCode = "INVALID_CREDENTIALS", message = ex.Message });
        }
    }
}