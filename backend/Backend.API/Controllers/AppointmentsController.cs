using RepairShop.Application.Modules.Appointments.Commands;
using RepairShop.Application.Modules.Appointments.Queries;
using RepairShop.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RepairShop.API.Controllers;

[ApiController]
[Route("api/appointments")]
public class AppointmentsController : ControllerBase
{
    private readonly IMediator _mediator;
    public AppointmentsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("available-slots")]
    [AllowAnonymous] // Guest/Customer cần xem trước khi đặt lịch
    public async Task<IActionResult> GetAvailableSlots([FromQuery] DateOnly date) =>
        Ok(await _mediator.Send(new GetAvailableSlotsQuery(date)));

    public record DeviceInfoBody(string? DeviceType, string? Brand, string? Model, string? IssueDescription);

    public record CreateAppointmentBody(
        string FullName, string Phone, DeviceInfoBody? DeviceInfo, DateOnly AppointmentDate, Guid TimeSlotId, Guid? CustomerId);

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Create(CreateAppointmentBody body)
    {
        var result = await _mediator.Send(new CreateAppointmentCommand(
            body.FullName, body.Phone, body.AppointmentDate, body.TimeSlotId, body.CustomerId,
            body.DeviceInfo?.DeviceType, body.DeviceInfo?.Brand, body.DeviceInfo?.Model, body.DeviceInfo?.IssueDescription));
        return CreatedAtAction(nameof(GetAvailableSlots), result);
    }

    [HttpGet("mine")]
    [Authorize(Roles = RepairShop.Domain.Common.Roles.Customer)]
    public async Task<IActionResult> Mine() => Ok(await _mediator.Send(new GetMyAppointmentsQuery()));

    [HttpPatch("{id:guid}/cancel")]
    [Authorize(Roles = RepairShop.Domain.Common.Roles.Customer)]
    public async Task<IActionResult> Cancel(Guid id) =>
        Ok(await _mediator.Send(new CancelAppointmentCommand(id)));

    [HttpPatch("{id:guid}/confirm")]
    [Authorize(Policy = AuthorizationPolicies.ReceptionistOrAdmin)]
    public async Task<IActionResult> Confirm(Guid id) => Ok(await _mediator.Send(new ConfirmAppointmentCommand(id)));

    [HttpPatch("{id:guid}/reject")]
    [Authorize(Policy = AuthorizationPolicies.ReceptionistOrAdmin)]
    public async Task<IActionResult> Reject(Guid id) => Ok(await _mediator.Send(new RejectAppointmentCommand(id)));

    public record ConvertBody(Guid CustomerId, Guid DeviceId);

    [HttpPost("{id:guid}/convert-to-ticket")]
    [Authorize(Policy = AuthorizationPolicies.ReceptionistOrAdmin)]
    public async Task<IActionResult> Convert(Guid id, ConvertBody body)
    {
        var result = await _mediator.Send(new ConvertAppointmentToTicketCommand(id, body.CustomerId, body.DeviceId));
        return Ok(result);
    }
}