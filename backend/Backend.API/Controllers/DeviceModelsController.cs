using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepairShop.Application.Modules.Devices;
using RepairShop.Infrastructure.Identity;

namespace RepairShop.API.Controllers;

[ApiController]
[Route("api/device-models")]
public class DeviceModelsController : ControllerBase
{
    private readonly IMediator _mediator;

    public DeviceModelsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicModels(
        [FromQuery] string? deviceType = null,
        [FromQuery] Guid? brandId = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100)
    {
        var result = await _mediator.Send(new GetDeviceModelsQuery(deviceType, brandId, true, search, page, pageSize));
        return Ok(result);
    }

    [HttpGet("admin")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> GetAdminModels(
        [FromQuery] string? deviceType = null,
        [FromQuery] Guid? brandId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100)
    {
        var result = await _mediator.Send(new GetDeviceModelsQuery(deviceType, brandId, isActive, search, page, pageSize));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetDeviceModelByIdQuery(id));
        return Ok(result);
    }

    public record UpsertDeviceModelBody(
        Guid BrandId,
        string DeviceType,
        string ModelName,
        string? ModelNumber = null,
        int? ReleaseYear = null,
        bool? IsActive = null);

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Create(UpsertDeviceModelBody body)
    {
        var result = await _mediator.Send(new CreateDeviceModelCommand(
            body.BrandId,
            body.DeviceType,
            body.ModelName,
            body.ModelNumber,
            body.ReleaseYear,
            body.IsActive ?? true));
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Update(Guid id, UpsertDeviceModelBody body)
    {
        var result = await _mediator.Send(new UpdateDeviceModelCommand(
            id,
            body.BrandId,
            body.DeviceType,
            body.ModelName,
            body.ModelNumber,
            body.ReleaseYear,
            body.IsActive));
        return Ok(result);
    }

    public record ToggleActiveBody(bool IsActive);

    [HttpPatch("{id:guid}/active")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> ToggleActive(Guid id, ToggleActiveBody body)
    {
        var result = await _mediator.Send(new ToggleDeviceModelActiveCommand(id, body.IsActive));
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteDeviceModelCommand(id));
        return NoContent();
    }
}
