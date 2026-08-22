using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Domain.Common;
using RepairShop.Domain.Modules.Tickets;

namespace RepairShop.Application.Common.Authorization;

/// <summary>
/// Task 2 Tuần 1 đã chốt: "Kỹ thuật viên được gán chỉ có thể xem/thao tác trên ticket đã được assign cho mình".
/// Đây là ownership check — khác hoàn toàn Role-based Authorization (Task 7 Tuần 3):
/// [Authorize(Roles="Technician")] chỉ xác nhận "user này CÓ VAI TRÒ Technician",
/// còn guard này xác nhận "user này CÓ PHẢI kỹ thuật viên ĐƯỢC GÁN cho đúng ticket này".
/// Admin/Receptionist không bị giới hạn — họ xem/thao tác được mọi ticket (đúng Actor Task 2 Tuần 1: Admin "xem toàn bộ ticket").
/// </summary>
public static class TicketAccessGuard
{
    public static void EnsureCanAccess(RepairTicket ticket, ICurrentUserService currentUser)
    {
        if (currentUser.Role != Roles.Technician)
            return; // Admin/Receptionist không bị giới hạn ownership

        if (ticket.TechnicianId != currentUser.UserId)
            throw new ForbiddenException(
                "Bạn chỉ được thao tác trên phiếu sửa chữa đã được phân công cho mình.");
    }
}