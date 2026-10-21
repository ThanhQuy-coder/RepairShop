using RepairShop.Application.Modules.Appointments.Commands;
using RepairShop.Application.Modules.Appointments.Queries;
using RepairShop.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RepairShop.Domain.Common;
using System.Collections.Concurrent;

namespace RepairShop.API.Controllers;

[ApiController]
[Route("api/appointments")]
public class AppointmentsController : ControllerBase
{
    private static readonly ConcurrentDictionary<string, Queue<DateTimeOffset>> LookupPhoneRequests = new();
    private readonly IMediator _mediator;
    public AppointmentsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("available-slots")]
    [AllowAnonymous] // Guest/Customer cần xem trước khi đặt lịch
    public async Task<IActionResult> GetAvailableSlots([FromQuery] DateOnly date, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetAvailableSlotsQuery(date), cancellationToken));

    [HttpGet("staff")]
    [Authorize(Policy = AuthorizationPolicies.ReceptionistOrAdmin)]
    public async Task<IActionResult> GetForStaff(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetStaffAppointmentsQuery(), cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ReceptionistOrAdmin)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetAppointmentDetailQuery(id), cancellationToken));

    public record DeviceInfoBody(string? DeviceType, string? Brand, string? Model, string? IssueDescription);

    public record CreateAppointmentBody(
        string FullName, string Phone, DeviceInfoBody? DeviceInfo, DateOnly AppointmentDate, Guid TimeSlotId, Guid? CustomerId);

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Create(CreateAppointmentBody body, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateAppointmentCommand(
            body.FullName, body.Phone, body.AppointmentDate, body.TimeSlotId, body.CustomerId,
            body.DeviceInfo?.DeviceType, body.DeviceInfo?.Brand, body.DeviceInfo?.Model, body.DeviceInfo?.IssueDescription), cancellationToken);
        return CreatedAtAction(nameof(GetAvailableSlots), result);
    }

    public record LookupBody(string LookupToken, string Phone);

    [HttpPost("lookup")]
    [AllowAnonymous]
    [EnableRateLimiting("PublicAppointmentLookupPolicy")]
    public async Task<IActionResult> Lookup(LookupBody body, CancellationToken cancellationToken) =>
        await LookupAppointmentAsync(body, cancellationToken);

    private async Task<IActionResult> LookupAppointmentAsync(LookupBody body, CancellationToken cancellationToken)
    {
        var phone = NormalizePhone(body.Phone);
        var now = DateTimeOffset.UtcNow;
        var requests = LookupPhoneRequests.GetOrAdd(phone, _ => new Queue<DateTimeOffset>());
        lock (requests)
        {
            while (requests.Count > 0 && now - requests.Peek() >= TimeSpan.FromMinutes(1))
                requests.Dequeue();
            if (requests.Count >= 5)
            {
                Response.Headers.RetryAfter = "60";
                return StatusCode(StatusCodes.Status429TooManyRequests, new { message = "Quá nhiều yêu cầu tra cứu. Vui lòng thử lại sau." });
            }
            requests.Enqueue(now);
        }

        return Ok(await _mediator.Send(new LookupAppointmentQuery(body.LookupToken, phone), cancellationToken));
    }

    private static string NormalizePhone(string phone) =>
        new(phone.Where(char.IsDigit).ToArray());

    [HttpGet("mine")]
    [Authorize(Roles = Roles.Customer)]
    public async Task<IActionResult> Mine(CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetMyAppointmentsQuery(), cancellationToken));

    public class CancelBody
    {
        public string? LookupToken { get; set; }
        public string? Phone { get; set; }
    }

    [HttpPatch("{id:guid}/cancel")]
    [AllowAnonymous]
    public async Task<IActionResult> Cancel(Guid id, CancelBody body) =>
        Ok(await _mediator.Send(new CancelAppointmentCommand(id,
            body.LookupToken,
            body.Phone)));

    [HttpPatch("{id:guid}/confirm")]
    [Authorize(Policy = AuthorizationPolicies.ReceptionistOrAdmin)]
    public async Task<IActionResult> Confirm(Guid id) => Ok(await _mediator.Send(new ConfirmAppointmentCommand(id)));

    public record RejectBody(string RejectionReason);

    [HttpPatch("{id:guid}/reject")]
    [Authorize(Policy = AuthorizationPolicies.ReceptionistOrAdmin)]
    public async Task<IActionResult> Reject(Guid id, RejectBody rejectBody) =>
        Ok(await _mediator.Send(new RejectAppointmentCommand(id, rejectBody.RejectionReason)));

    [HttpPatch("{id:guid}/no-show")]
    [Authorize(Policy = AuthorizationPolicies.ReceptionistOrAdmin)]
    public async Task<IActionResult> MarkNoShow(Guid id)
        => Ok(await _mediator.Send(new MarkNoShowAppointmentCommand(id)));

    public record ConvertBody(Guid CustomerId, Guid DeviceId);

    [HttpPost("{id:guid}/convert-to-ticket")]
    [Authorize(Policy = AuthorizationPolicies.ReceptionistOrAdmin)]
    public async Task<IActionResult> Convert(Guid id, ConvertBody body)
    {
        var result = await _mediator.Send(new ConvertAppointmentToTicketCommand(id, body.CustomerId, body.DeviceId));
        return Ok(result);
    }

    [HttpPatch("{id:guid}/mark-completed")]
    [Authorize(Policy = AuthorizationPolicies.ReceptionistOrAdmin)]
    public async Task<IActionResult> MarkCompleted(Guid id)
        => Ok(await _mediator.Send(new CompleteAppointmentCommand(id)));
}