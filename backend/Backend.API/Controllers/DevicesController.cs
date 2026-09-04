using RepairShop.Application.Modules.Devices.Commands;
using RepairShop.Application.Modules.Devices.Queries;
using RepairShop.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RepairShop.API.Controllers;

[ApiController]
[Route("api/devices")]
public class DevicesController : ControllerBase
{
    private readonly IMediator _mediator;

    public DevicesController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.StaffOnly)] // Receptionist, Admin, Technician (API Spec)
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetDeviceByIdQuery(id));
        return Ok(result);
    }

    [HttpGet("by-customer/{customerId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.StaffOnly)]
    public async Task<IActionResult> GetByCustomer(Guid customerId)
    {
        var result = await _mediator.Send(new GetDevicesByCustomerQuery(customerId));
        return Ok(result);
    }

    [HttpGet("{id:guid}/repair-history")]
    [Authorize]
    public async Task<IActionResult> GetRepairHistory(Guid id)
    {
        var result = await _mediator.Send(new GetDeviceRepairHistoryQuery(id));
        return Ok(new { items = result });
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ReceptionistOrAdmin)] // FR-011
    public async Task<IActionResult> Create(CreateDeviceCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ReceptionistOrAdmin)] // FR-012
    public async Task<IActionResult> Update(Guid id, UpdateDeviceCommand command)
    {
        if (id != command.Id) return BadRequest(new { success = false, message = "Id không khớp." });
        var result = await _mediator.Send(command);
        return Ok(result);
    }
}