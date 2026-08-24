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

    public record RepairNoteBody(string Note);

    [HttpPost("{id:guid}/repair-notes")]
    [Authorize(Roles = Roles.Technician)]
    public async Task<IActionResult> AddRepairNote(Guid id, RepairNoteBody body)
    {
        await _mediator.Send(new AddRepairNoteCommand(id, body.Note));
        return NoContent();
    }

    public record UsePartBody(Guid PartId, int Quantity);

    [HttpPost("{id:guid}/parts")]
    [Authorize(Roles = Roles.Technician)] // FR-043
    public async Task<IActionResult> UsePart(Guid id, UsePartBody body)
    {
        var result = await _mediator.Send(new UsePartCommand(id, body.PartId, body.Quantity));
        return Ok(result);
    }

    public record CompletionNotesBody(string CompletionNotes);

    [HttpPatch("{id:guid}/completion-notes")]
    [Authorize(Roles = Roles.Technician)]
    public async Task<IActionResult> RecordCompletionNotes(Guid id, CompletionNotesBody body)
    {
        await _mediator.Send(new RecordCompletionNotesCommand(id, body.CompletionNotes));
        return NoContent();
    }

    [HttpPatch("{id:guid}/start-qa")]
    [Authorize(Roles = Roles.Technician)]
    public async Task<IActionResult> StartQualityCheck(Guid id)
    {
        var result = await _mediator.Send(new StartQualityCheckCommand(id));
        return Ok(result);
    }

    public record PassQaBody(string FunctionalCheckNotes, string CosmeticCheckNotes, string OriginalIssueResolvedNotes);

    [HttpPatch("{id:guid}/qa-pass")]
    [Authorize(Roles = Roles.Technician)]
    public async Task<IActionResult> PassQualityCheck(Guid id, PassQaBody body)
    {
        var result = await _mediator.Send(new PassQualityCheckCommand(id,
            body.FunctionalCheckNotes, body.CosmeticCheckNotes, body.OriginalIssueResolvedNotes));
        return Ok(result);
    }

    public record FailQaBody(string FailureReason);

    [HttpPatch("{id:guid}/qa-fail")]
    [Authorize(Roles = Roles.Technician)]
    public async Task<IActionResult> FailQualityCheck(Guid id, FailQaBody body)
    {
        var result = await _mediator.Send(new FailQualityCheckCommand(id, body.FailureReason));
        return Ok(result);
    }

    public record CreateInvoiceBody(string PaymentMethod);

    [HttpPost("{id:guid}/invoice")]
    [Authorize(Policy = AuthorizationPolicies.ReceptionistOrAdmin)]
    public async Task<IActionResult> CreateInvoice(Guid id, CreateInvoiceBody body)
    {
        var result = await _mediator.Send(new CreateInvoiceCommand(id, body.PaymentMethod));
        return Ok(result);
    }

    public record DeliverBody(string? DeliveryNote);

    [HttpPatch("{id:guid}/deliver")]
    [Authorize(Policy = AuthorizationPolicies.ReceptionistOrAdmin)]
    public async Task<IActionResult> Deliver(Guid id, DeliverBody body)
    {
        var result = await _mediator.Send(new DeliverTicketCommand(id, body.DeliveryNote));
        return Ok(result);
    }

    [HttpGet("{id:guid}/status-history")]
    [Authorize(Policy = AuthorizationPolicies.StaffOnly)]
    public async Task<IActionResult> GetStatusHistory(Guid id)
    {
        var result = await _mediator.Send(new GetTicketStatusHistoryQuery(id));
        return Ok(result);
    }

    public record CreateWarrantyBody(int WarrantyMonths, string? Terms);

    [HttpPost("{id:guid}/warranty")]
    [Authorize(Policy = AuthorizationPolicies.ReceptionistOrAdmin)] // FR-036
    public async Task<IActionResult> CreateWarranty(Guid id, CreateWarrantyBody body)
    {
        var result = await _mediator.Send(new CreateWarrantyCommand(id, body.WarrantyMonths, body.Terms));
        return Ok(result);
    }

    [HttpGet("{id:guid}/warranty")]
    [Authorize(Policy = AuthorizationPolicies.StaffOnly)] // Customer (ticket của mình) để dành khi làm ownership Quote-tương tự
    public async Task<IActionResult> GetWarranty(Guid id)
    {
        var result = await _mediator.Send(new GetWarrantyByTicketQuery(id));
        return Ok(result);
    }
}