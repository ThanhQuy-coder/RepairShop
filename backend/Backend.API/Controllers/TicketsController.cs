using RepairShop.Application.Modules.Tickets.Commands;
using RepairShop.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepairShop.Domain.Modules.Tickets.Enums;
using RepairShop.Domain.Common;

namespace RepairShop.API.Controllers;

[ApiController]
[Route("api/tickets")]
public class TicketsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TicketsController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ReceptionistOrAdmin)] // FR-015: Receptionist tạo ticket
    public async Task<IActionResult> Create(CreateTicketCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(Create), new { id = result.Id }, result);
        // Lưu ý: chưa có GET /api/tickets/{id} thật (thuộc Task khác trong Tuần 4) nên tạm route lại chính action này —
        // sẽ đổi thành nameof(GetById) khi task đó hoàn thành.
    }

    [HttpPatch("{id:guid}/intake-condition")]
    [Authorize(Policy = AuthorizationPolicies.ReceptionistOrAdmin)]
    public async Task<IActionResult> UpdateIntakeCondition(Guid id, UpdateConditionBody body)
    {
        await _mediator.Send(new UpdateIntakeConditionCommand(id, body.ConditionNotes, body.RiskWarning));
        return NoContent();
    }

    public record UpdateConditionBody(string? ConditionNotes, string? RiskWarning);

    [HttpPost("{id:guid}/images")]
    [Authorize(Policy = AuthorizationPolicies.StaffOnly)] // Domain tự phân biệt hợp lệ theo trạng thái, Controller chỉ chặn "phải là nhân viên"
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadImage(Guid id, IFormFile file, [FromForm] ImageType imageType,
    [FromForm] string? caption)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { success = false, message = "File ảnh không được để trống." });

        await using var stream = file.OpenReadStream();
        var result = await _mediator.Send(new UploadTicketImageCommand(id, stream, file.FileName, imageType, caption));
        return Ok(result);
    }

    [HttpGet("{id:guid}/images")]
    [Authorize(Policy = AuthorizationPolicies.StaffOnly)]
    public async Task<IActionResult> GetImages(Guid id)
    {
        var result = await _mediator.Send(new GetTicketImagesQuery(id));
        return Ok(result);
    }
    public record AssignTechnicianBody(Guid TechnicianId, string? Note);

    [HttpPatch("{id:guid}/assign-technician")]
    [Authorize(Policy = AuthorizationPolicies.ReceptionistOrAdmin)] // FR-018: Receptionist/Admin gán kỹ thuật viên
    public async Task<IActionResult> AssignTechnician(Guid id, AssignTechnicianBody body)
    {
        var result = await _mediator.Send(new AssignTechnicianCommand(id, body.TechnicianId, body.Note));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.StaffOnly)] // Role check: phải là nhân viên nội bộ (Task 7)
    public async Task<IActionResult> GetById(Guid id)
    {
        // Ownership check (khác role check) nằm TRONG Handler, không phải ở đây
        var result = await _mediator.Send(new GetTicketByIdQuery(id));
        return Ok(result);
    }

    [HttpPatch("{id:guid}/start-diagnosis")]
    [Authorize(Roles = Roles.Technician)] // chỉ Technician thực hiện chẩn đoán (FR-024)
    public async Task<IActionResult> StartDiagnosis(Guid id)
    {
        var result = await _mediator.Send(new StartDiagnosisCommand(id));
        return Ok(result);
    }

    public record SubmitDiagnosisBody(string DiagnosisResult, string? RootCause,
        string? RecommendedRepair, string? RequiredPartsNote, string? TechnicalNote);

    [HttpPatch("{id:guid}/diagnosis")]
    [Authorize(Roles = Roles.Technician)]
    public async Task<IActionResult> SubmitDiagnosis(Guid id, SubmitDiagnosisBody body)
    {
        var result = await _mediator.Send(new SubmitDiagnosisCommand(id, body.DiagnosisResult,
            body.RootCause, body.RecommendedRepair, body.RequiredPartsNote, body.TechnicalNote));
        return Ok(result);
    }

    [HttpPost("{id:guid}/quotes")]
    [Authorize(Policy = AuthorizationPolicies.ReceptionistOrAdmin)] // FR-030
    public async Task<IActionResult> CreateQuote(Guid id, CreateQuoteBody body)
    {
        var result = await _mediator.Send(new CreateQuoteCommand(id, body.Description, body.Items));
        return CreatedAtAction(nameof(GetQuotes), new { id }, result);
    }

    [HttpGet("{id:guid}/quotes")]
    [Authorize] // Receptionist/Admin/Technician/Customer (ticket của mình) — ownership check để dành khi làm GET tổng quát
    public async Task<IActionResult> GetQuotes(Guid id)
    {
        var result = await _mediator.Send(new GetTicketQuotesQuery(id));
        return Ok(result);
    }

    public record CreateQuoteBody(string Description, List<QuoteItemInput> Items);
}