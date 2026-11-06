using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepairShop.Application.Modules.Devices;
using RepairShop.Infrastructure.Identity;

namespace RepairShop.API.Controllers;

[ApiController]
[Route("api/brands")]
public class BrandsController : ControllerBase
{
    private readonly IMediator _mediator;

    public BrandsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicBrands(
        [FromQuery] string? deviceType = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100)
    {
        var result = await _mediator.Send(new GetBrandsQuery(deviceType, true, search, page, pageSize));
        return Ok(result);
    }

    [HttpGet("admin")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> GetAdminBrands(
        [FromQuery] string? deviceType = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100)
    {
        var result = await _mediator.Send(new GetBrandsQuery(deviceType, isActive, search, page, pageSize));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetBrandByIdQuery(id));
        return Ok(result);
    }

    public record UpsertBrandBody(
        string Name,
        string Code,
        string? LogoUrl = null,
        string? Description = null,
        bool? IsActive = null);

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Create(UpsertBrandBody body)
    {
        var result = await _mediator.Send(new CreateBrandCommand(
            body.Name,
            body.Code,
            body.LogoUrl,
            body.Description,
            body.IsActive ?? true));
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Update(Guid id, UpsertBrandBody body)
    {
        var result = await _mediator.Send(new UpdateBrandCommand(
            id,
            body.Name,
            body.Code,
            body.LogoUrl,
            body.Description,
            body.IsActive));
        return Ok(result);
    }

    public record ToggleActiveBody(bool IsActive);

    [HttpPatch("{id:guid}/active")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> ToggleActive(Guid id, ToggleActiveBody body)
    {
        var result = await _mediator.Send(new ToggleBrandActiveCommand(id, body.IsActive));
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteBrandCommand(id));
        return NoContent();
    }
}
