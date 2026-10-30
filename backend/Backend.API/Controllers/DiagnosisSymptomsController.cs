using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepairShop.Application.Modules.Diagnostics.Commands;
using RepairShop.Application.Modules.Diagnostics.Queries;
using RepairShop.Domain.Common.Enums;
using RepairShop.Infrastructure.Identity;

namespace RepairShop.API.Controllers;

[ApiController]
[Route("api/diagnosis-symptoms")]
public class DiagnosisSymptomsController : ControllerBase
{
    private readonly IMediator _mediator;
    public DiagnosisSymptomsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.TechnicianOrAdmin)]
    public async Task<IActionResult> Get([FromQuery] DeviceType? deviceType) =>
        Ok(await _mediator.Send(new GetDiagnosisSymptomsQuery(deviceType, true)));

    [HttpGet("admin")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> GetAll([FromQuery] DeviceType? deviceType, [FromQuery] bool? isActive) =>
        Ok(await _mediator.Send(new GetDiagnosisSymptomsQuery(deviceType, isActive)));

    public record DiagnosisSymptomBody(string Name, DeviceType DeviceType, int SortOrder);

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Create(DiagnosisSymptomBody body)
    {
        var result = await _mediator.Send(new CreateDiagnosisSymptomCommand(body.Name, body.DeviceType, body.SortOrder));
        return CreatedAtAction(nameof(GetAll), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Update(Guid id, DiagnosisSymptomBody body) =>
        Ok(await _mediator.Send(new UpdateDiagnosisSymptomCommand(id, body.Name, body.DeviceType, body.SortOrder)));

    public record ToggleBody(bool IsActive);

    [HttpPatch("{id:guid}/active")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Toggle(Guid id, ToggleBody body)
    {
        await _mediator.Send(new ToggleDiagnosisSymptomCommand(id, body.IsActive));
        return NoContent();
    }
}
