using RepairShop.Application.Modules.Appointments.Commands;
using RepairShop.Application.Modules.Appointments.Queries;
using RepairShop.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RepairShop.API.Controllers;

[ApiController]
[Route("api/time-slots")]
public class TimeSlotsController : ControllerBase
{
    private readonly IMediator _mediator;
    public TimeSlotsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [AllowAnonymous] // Customer cần thấy khung giờ khi đặt lịch (Task 10.4 dùng lại danh sách này)
    public async Task<IActionResult> GetAll() => Ok(await _mediator.Send(new GetTimeSlotsQuery()));

    public record CreateTimeSlotBody(int? DayOfWeek, TimeOnly SlotStart, TimeOnly SlotEnd, int MaxCapacity);

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Create(CreateTimeSlotBody body)
    {
        var result = await _mediator.Send(new CreateTimeSlotCommand(body.DayOfWeek, body.SlotStart, body.SlotEnd, body.MaxCapacity));
        return CreatedAtAction(nameof(GetAll), result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Update(Guid id, CreateTimeSlotBody body)
    {
        var result = await _mediator.Send(new UpdateTimeSlotCommand(id, body.DayOfWeek, body.SlotStart, body.SlotEnd, body.MaxCapacity));
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteTimeSlotCommand(id));
        return NoContent();
    }
}