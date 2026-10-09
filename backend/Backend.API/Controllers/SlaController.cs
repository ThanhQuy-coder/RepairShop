using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RepairShop.Domain.Common.Enums;
using RepairShop.Domain.Modules.SLA;
using RepairShop.Infrastructure.Identity;
using RepairShop.Infrastructure.Persistence;

namespace RepairShop.API.Controllers;

[ApiController]
[Route("api/sla")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class SlaController(AppDbContext db) : ControllerBase
{
    [HttpGet("policies")]
    public async Task<ActionResult<IReadOnlyList<SlaPolicyResponse>>> GetPolicies(CancellationToken ct)
        => Ok(await db.SLAPolicies.AsNoTracking()
            .Select(x => new SlaPolicyResponse(x.Id, x.StatusCode, x.DeviceType, x.DurationMinutes, x.IsActive))
            .ToListAsync(ct));

    [HttpPost("policies")]
    public async Task<ActionResult<SlaPolicyResponse>> CreatePolicy(
        CreateSlaPolicyRequest request, CancellationToken ct)
    {
        if (request.DurationMinutes <= 0) return BadRequest("DurationMinutes phải lớn hơn 0.");
        if (await db.SLAPolicies.AnyAsync(x => x.StatusCode == request.StatusCode &&
            x.DeviceType == request.DeviceType, ct))
            return Conflict("Đã tồn tại chính sách SLA cho tổ hợp này.");

        var policy = new SLAPolicy(request.StatusCode, request.DeviceType, request.DurationMinutes);
        db.SLAPolicies.Add(policy);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetPolicies), null,
            new SlaPolicyResponse(policy.Id, policy.StatusCode, policy.DeviceType,
                policy.DurationMinutes, policy.IsActive));
    }

    [HttpPut("policies/{id:guid}")]
    public async Task<IActionResult> UpdatePolicy(Guid id, UpdateSlaPolicyRequest request, CancellationToken ct)
    {
        var policy = await db.SLAPolicies.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (policy is null) return NotFound();
        policy.Update(request.DurationMinutes, request.IsActive);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    public sealed record CreateSlaPolicyRequest(string StatusCode, DeviceType? DeviceType, int DurationMinutes);
    public sealed record UpdateSlaPolicyRequest(int DurationMinutes, bool IsActive);
    public sealed record SlaPolicyResponse(Guid Id, string StatusCode, DeviceType? DeviceType, int DurationMinutes, bool IsActive);
}
