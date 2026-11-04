using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.SLA.DTOs;
using RepairShop.Domain.Common.Enums;
using RepairShop.Infrastructure.Identity;

namespace RepairShop.API.Controllers;

[ApiController]
[Route("api/sla")]
[Authorize(Policy = AuthorizationPolicies.StaffOnly)]
public sealed class SlaDashboardController(ISlaQueryService queryService) : ControllerBase
{
    [HttpGet("tickets")]
    public async Task<ActionResult<SlaTicketListResponse>> GetTickets(
        [FromQuery] string? slaStatus,
        [FromQuery] string? statusCode,
        [FromQuery] string? deviceType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseStatus(slaStatus, out var parsedStatus))
            return BadRequest("slaStatus không hợp lệ.");
        if (!TryParseDeviceType(deviceType, out var parsedDeviceType))
            return BadRequest("deviceType không hợp lệ.");

        return Ok(await queryService.GetTicketsAsync(
            parsedStatus, statusCode, parsedDeviceType, page, pageSize, cancellationToken));
    }

    [HttpGet("my-tickets")]
    [Authorize(Policy = AuthorizationPolicies.TechnicianOrAdmin)]
    public async Task<ActionResult<SlaMyTicketsResponse>> GetMyTickets(
        CancellationToken cancellationToken)
        => Ok(await queryService.GetMyTicketsAsync(cancellationToken));

    [HttpGet("/api/reports/sla-analytics")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<ActionResult<SlaAnalyticsResponse>> GetAnalytics(
        CancellationToken cancellationToken)
        => Ok(await queryService.GetAnalyticsAsync(cancellationToken));

    private static bool TryParseStatus(string? value, out SLAStatus? status)
    {
        status = null;
        if (string.IsNullOrWhiteSpace(value)) return true;
        if (!Enum.TryParse<SLAStatus>(value, ignoreCase: true, out var parsed)) return false;
        status = parsed;
        return true;
    }

    private static bool TryParseDeviceType(string? value, out DeviceType? deviceType)
    {
        deviceType = null;
        if (string.IsNullOrWhiteSpace(value)) return true;
        if (!Enum.TryParse<DeviceType>(value, ignoreCase: true, out var parsed)) return false;
        deviceType = parsed;
        return true;
    }
}
