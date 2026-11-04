using Microsoft.EntityFrameworkCore;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.SLA.DTOs;
using RepairShop.Domain.Common;
using RepairShop.Domain.Common.Enums;
using RepairShop.Domain.Modules.SLA;

namespace RepairShop.Infrastructure.Persistence.Repositories;

public sealed class SlaQueryService(
    AppDbContext context,
    ICurrentUserService currentUser) : ISlaQueryService
{
    public async Task<SlaSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var query = context.TicketSLAs
            .Where(sla => sla.Status != SLAStatus.Completed);

        if (currentUser.Role == Roles.Technician && currentUser.UserId is Guid technicianId)
            query = query.Where(sla => sla.RepairTicket.TechnicianId == technicianId);
        else if (currentUser.Role == Roles.Receptionist && currentUser.UserId is Guid receptionistId)
            query = query.Where(sla => sla.RepairTicket.ReceptionistId == receptionistId);

        var counts = await query
            .GroupBy(sla => sla.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        int Count(SLAStatus status)
            => counts.FirstOrDefault(item => item.Status == status)?.Count ?? 0;

        return new SlaSummaryResponse(
            Count(SLAStatus.OnTrack),
            Count(SLAStatus.DueSoon),
            Count(SLAStatus.Overdue),
            counts.Sum(item => item.Count));
    }

    public async Task<SlaTicketListResponse> GetTicketsAsync(
        SLAStatus? slaStatus, string? statusCode, DeviceType? deviceType,
        int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = BuildScopedQuery();
        if (slaStatus is not null)
            query = query.Where(sla => sla.Status == slaStatus);
        if (!string.IsNullOrWhiteSpace(statusCode))
            query = query.Where(sla => sla.StatusCode == statusCode);
        if (deviceType is not null)
            query = query.Where(sla => sla.RepairTicket.Device.DeviceType == deviceType);

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(sla => sla.Status == SLAStatus.Overdue ? 0 : 1)
            .ThenBy(sla => sla.DueAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToItem())
            .ToListAsync(cancellationToken);

        return new SlaTicketListResponse(items, total);
    }

    public async Task<SlaAnalyticsResponse> GetAnalyticsAsync(
        CancellationToken cancellationToken)
    {
        var active = context.TicketSLAs
            .AsNoTracking()
            .Where(sla => sla.Status != SLAStatus.Completed);

        var total = await active.CountAsync(cancellationToken);
        var onTrack = await active.CountAsync(sla => sla.Status == SLAStatus.OnTrack, cancellationToken);
        var dueSoon = await active.CountAsync(sla => sla.Status == SLAStatus.DueSoon, cancellationToken);
        var overdue = await active.CountAsync(sla => sla.Status == SLAStatus.Overdue, cancellationToken);
        var completed = context.TicketSLAs.AsNoTracking()
            .Where(sla => sla.Status == SLAStatus.Completed);
        var completedOnTime = await completed.CountAsync(
            sla => sla.CompletedAt <= sla.DueAt, cancellationToken);
        var complianceTotal = total + await completed.CountAsync(cancellationToken);
        var complianceRate = complianceTotal == 0
            ? 100m
            : Math.Round((decimal)(onTrack + completedOnTime) * 100 / complianceTotal, 1);

        var stepRows = await active
            .Select(sla => new
            {
                sla.StatusCode,
                Label = sla.RepairTicket.Status.Name,
                sla.Status
            })
            .GroupBy(item => new { item.StatusCode, item.Label, item.Status })
            .Select(group => new
            {
                group.Key.StatusCode,
                group.Key.Label,
                group.Key.Status,
                Count = group.Count()
            })
            .ToListAsync(cancellationToken);
        var byStep = stepRows
            .GroupBy(item => new { item.StatusCode, item.Label })
            .Select(group => new SlaStepSummary(
                group.Key.StatusCode,
                group.Key.Label,
                group.Sum(item => item.Count),
                group.Where(item => item.Status == SLAStatus.Overdue)
                    .Sum(item => item.Count)))
            .OrderByDescending(item => item.Overdue)
            .ToList();

        var technicianRows = await context.TicketSLAs
            .AsNoTracking()
            .Where(sla => sla.RepairTicket.TechnicianId != null)
            .Select(sla => new
            {
                TechnicianName = sla.RepairTicket.Technician!.FullName,
                sla.Status,
                IsCompletedOnTime = sla.CompletedAt != null && sla.CompletedAt <= sla.DueAt
            })
            .GroupBy(item => new
            {
                item.TechnicianName,
                item.Status,
                item.IsCompletedOnTime
            })
            .Select(group => new
            {
                group.Key.TechnicianName,
                group.Key.Status,
                group.Key.IsCompletedOnTime,
                Count = group.Count()
            })
            .ToListAsync(cancellationToken);
        var byTechnician = technicianRows
            .GroupBy(item => item.TechnicianName)
            .Select(group =>
            {
                var total = group.Sum(item => item.Count);
                var onTime = group
                    .Where(item => item.Status == SLAStatus.OnTrack || item.IsCompletedOnTime)
                    .Sum(item => item.Count);
                return new SlaTechnicianSummary(
                    group.Key,
                    group.Where(item => item.Status != SLAStatus.Completed)
                        .Sum(item => item.Count),
                    group.Where(item => item.Status == SLAStatus.DueSoon)
                        .Sum(item => item.Count),
                    group.Where(item => item.Status == SLAStatus.Overdue)
                        .Sum(item => item.Count),
                    total == 0 ? 100m : Math.Round(onTime * 100m / total, 1));
            })
            .OrderByDescending(item => item.Overdue)
            .ToList();

        var deviceRows = await active
            .Select(sla => new
            {
                DeviceType = sla.RepairTicket.Device.DeviceType,
                sla.Status
            })
            .GroupBy(item => new { item.DeviceType, item.Status })
            .Select(group => new
            {
                group.Key.DeviceType,
                group.Key.Status,
                Count = group.Count()
            })
            .ToListAsync(cancellationToken);
        var byDevice = deviceRows
            .GroupBy(item => item.DeviceType)
            .Select(group => new SlaDeviceSummary(
                group.Key.ToString(),
                group.Sum(item => item.Count),
                group.Where(item => item.Status == SLAStatus.Overdue)
                    .Sum(item => item.Count)))
            .OrderByDescending(item => item.Overdue)
            .ToList();

        return new SlaAnalyticsResponse(total, onTrack, dueSoon, overdue,
            complianceRate, byStep, byTechnician, byDevice);
    }

    public async Task<SlaMyTicketsResponse> GetMyTicketsAsync(
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid technicianId)
            return new SlaMyTicketsResponse(0, 0, 0, [], []);

        var active = BuildScopedQuery()
            .Where(sla => sla.RepairTicket.TechnicianId == technicianId);
        var items = await active
            .OrderBy(sla => sla.Status == SLAStatus.Overdue ? 0 : 1)
            .ThenBy(sla => sla.DueAt)
            .Select(ToItem())
            .ToListAsync(cancellationToken);
        var completed = await context.TicketSLAs
            .AsNoTracking()
            .Where(sla => sla.Status == SLAStatus.Completed &&
                sla.RepairTicket.TechnicianId == technicianId)
            .OrderByDescending(sla => sla.CompletedAt)
            .Take(20)
            .Select(sla => new SlaCompletedItem(
                new SlaTicketItem(
                    sla.RepairTicketId,
                    sla.RepairTicket.TicketCode,
                    sla.RepairTicket.Customer.FullName,
                    sla.RepairTicket.Device.Brand + " " + sla.RepairTicket.Device.Model,
                    sla.StatusCode,
                    sla.RepairTicket.Status.Name,
                    sla.RepairTicket.Technician == null
                        ? null
                        : sla.RepairTicket.Technician.FullName,
                    sla.StartedAt,
                    sla.DueAt,
                    sla.Status,
                    false),
                sla.CompletedAt <= sla.DueAt))
            .ToListAsync(cancellationToken);

        return new SlaMyTicketsResponse(
            items.Count(item => item.SlaStatus == SLAStatus.Overdue),
            items.Count(item => item.SlaStatus == SLAStatus.DueSoon),
            items.Count(item => item.SlaStatus == SLAStatus.OnTrack),
            items, completed);
    }

    private IQueryable<TicketSLA> BuildScopedQuery()
    {
        var query = context.TicketSLAs
            .AsNoTracking()
            .Where(sla => sla.Status != SLAStatus.Completed);

        if (currentUser.Role == Roles.Technician && currentUser.UserId is Guid technicianId)
            query = query.Where(sla => sla.RepairTicket.TechnicianId == technicianId);
        else if (currentUser.Role == Roles.Receptionist && currentUser.UserId is Guid receptionistId)
            query = query.Where(sla => sla.RepairTicket.ReceptionistId == receptionistId);

        return query;
    }

    private static System.Linq.Expressions.Expression<
        Func<TicketSLA, SlaTicketItem>> ToItem()
        => sla => new SlaTicketItem(
            sla.RepairTicketId,
            sla.RepairTicket.TicketCode,
            sla.RepairTicket.Customer.FullName,
            sla.RepairTicket.Device.Brand + " " + sla.RepairTicket.Device.Model,
            sla.StatusCode,
            sla.RepairTicket.Status.Name,
            sla.RepairTicket.Technician == null
                ? null
                : sla.RepairTicket.Technician.FullName,
            sla.StartedAt,
            sla.DueAt,
            sla.Status,
            sla.Status != SLAStatus.Completed);
}
