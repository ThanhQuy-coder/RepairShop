using RepairShop.Application.Modules.Content.Commands;
using RepairShop.Application.Modules.Content.Queries;
using RepairShop.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RepairShop.API.Controllers;

[ApiController]
[Route("api/services")]
public class ServicesController : ControllerBase
{
    private readonly IMediator _mediator;
    public ServicesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [AllowAnonymous] // FR-049: Customer xem công khai, chỉ thấy isActive=true
    public async Task<IActionResult> GetPublicServices([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var result = await _mediator.Send(new GetServicesQuery(true, page, pageSize));
        return Ok(result);
    }

    [HttpGet("admin")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)] // Admin xem cả bản Unpublish
    public async Task<IActionResult> GetAllServices([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var result = await _mediator.Send(new GetServicesQuery(null, page, pageSize));
        return Ok(result);
    }

    public record CreateServiceBody(string Name, string? Description, decimal? BasePrice, string? DeviceType);

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Create(CreateServiceBody body)
    {
        var result = await _mediator.Send(new CreateServiceCommand(body.Name, body.Description, body.BasePrice, body.DeviceType));
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Update(Guid id, CreateServiceBody body)
    {
        var result = await _mediator.Send(new UpdateServiceCommand(id, body.Name, body.Description, body.BasePrice, body.DeviceType));
        return Ok(result);
    }

    public record TogglePublishBody(bool Publish);

    [HttpPatch("{id:guid}/publish")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> TogglePublish(Guid id, TogglePublishBody body)
    {
        await _mediator.Send(new ToggleServicePublishCommand(id, body.Publish));
        return NoContent();
    }
}