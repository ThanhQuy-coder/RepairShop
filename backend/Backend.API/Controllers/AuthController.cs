using RepairShop.Application.Modules.Identity.Queries;
using Microsoft.AspNetCore.Authorization;
using RepairShop.Application.Modules.Identity.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Identity.DTOs;

namespace RepairShop.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IRefreshTokenService _refreshTokens;

    public AuthController(IMediator mediator, IRefreshTokenService refreshTokens)
    {
        _mediator = mediator;
        _refreshTokens = refreshTokens;
    }

    private static readonly CookieOptions RefreshCookieOptions = new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/api/auth"
    };

    private void SetRefreshCookie(string value, DateTimeOffset expires)
    {
        Response.Cookies.Append("repairshop_refresh_token", value, new CookieOptions
        {
            HttpOnly = RefreshCookieOptions.HttpOnly,
            Secure = RefreshCookieOptions.Secure,
            SameSite = RefreshCookieOptions.SameSite,
            Path = RefreshCookieOptions.Path,
            Expires = expires
        });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterCommand command)
    {
        var result = await _mediator.Send(command);
        var session = await _refreshTokens.IssueAsync(result.Email);
        SetRefreshCookie(session.RefreshToken, DateTimeOffset.UtcNow.AddDays(30));
        return Ok(session.Response);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginCommand command)
    {
        var result = await _mediator.Send(command);
        var session = await _refreshTokens.IssueAsync(result.Email);
        SetRefreshCookie(session.RefreshToken, DateTimeOffset.UtcNow.AddDays(30));
        return Ok(session.Response);
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken()
    {
        if (!Request.Cookies.TryGetValue("repairshop_refresh_token", out var token) ||
            string.IsNullOrWhiteSpace(token))
            return Unauthorized();

        var session = await _refreshTokens.RotateAsync(token);
        SetRefreshCookie(session.RefreshToken, DateTimeOffset.UtcNow.AddDays(30));
        return Ok(session.Response);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout()
    {
        if (Request.Cookies.TryGetValue("repairshop_refresh_token", out var token) &&
            !string.IsNullOrWhiteSpace(token))
            await _refreshTokens.RevokeAsync(token);

        Response.Cookies.Delete("repairshop_refresh_token", new CookieOptions
        {
            HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Path = "/api/auth"
        });
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var result = await _mediator.Send(new GetProfileQuery());
        return Ok(result);
    }
}