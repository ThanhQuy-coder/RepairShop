using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Domain.Common;
using RepairShop.Domain.Modules.Customers;
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
    /// <summary>Dùng cho Technician (Task 4.6) — chỉ thao tác được ticket đã assign cho mình.</summary>
    public static void EnsureCanAccess(RepairTicket ticket, ICurrentUserService currentUser)
    {
        if (currentUser.Role == Roles.Technician && ticket.TechnicianId != currentUser.UserId)
            throw new ForbiddenException("Bạn chỉ được thao tác trên phiếu sửa chữa đã được phân công cho mình.");

        if (currentUser.Role == Roles.Customer)
            throw new ForbiddenException("Khách hàng không có quyền thao tác nghiệp vụ trên phiếu sửa chữa.");
    }

    /// <summary>
    /// Dùng cho Customer xem ticket của CHÍNH MÌNH (GET /tickets/{id}, GET quotes/warranty theo ticket...).
    /// customer = null nghĩa là user hiện tại chưa có hồ sơ Customer liên kết -> chắc chắn không sở hữu gì.
    /// </summary>
    public static void EnsureCustomerOwnsTicket(RepairTicket ticket, Customer? customer)
    {
        if (customer is null || ticket.CustomerId != customer.Id)
            throw new ForbiddenException("Bạn không có quyền truy cập phiếu sửa chữa này.");
    }
}