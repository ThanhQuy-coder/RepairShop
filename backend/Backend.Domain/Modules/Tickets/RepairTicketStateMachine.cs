using RepairShop.Domain.Common;
using RepairShop.Domain.Common.Exceptions;

namespace RepairShop.Domain.Modules.Tickets;

public static class RepairTicketStateMachine
{
    private static readonly Dictionary<string, string[]> AllowedTransitions = new()
    {
        [RepairStatusCodes.CheckedIn] = [RepairStatusCodes.Assigned],
        [RepairStatusCodes.Assigned] = [RepairStatusCodes.Diagnosing],
        [RepairStatusCodes.Diagnosing] = [RepairStatusCodes.WaitingApproval],

        // Bước 7 (Task 1 Tuần 1) — 3 nhánh rẽ: đồng ý / từ chối / trì hoãn (On-hold)
        [RepairStatusCodes.WaitingApproval] =
        [
            RepairStatusCodes.InRepair,
            RepairStatusCodes.WaitingParts,
            RepairStatusCodes.ClosedRejected,
            RepairStatusCodes.OnHold
        ],
        [RepairStatusCodes.OnHold] = [RepairStatusCodes.WaitingApproval], // khách quyết định lại sau khi trì hoãn

        [RepairStatusCodes.WaitingParts] = [RepairStatusCodes.InRepair],        // đủ linh kiện → sửa tiếp
        [RepairStatusCodes.InRepair] = [RepairStatusCodes.QaTesting],

        // QA có 2 kết quả: đạt → ReadyForPickup, KHÔNG đạt → quay lại InRepair (bước 10, Task 1 Tuần 1)
        [RepairStatusCodes.QaTesting] = [RepairStatusCodes.ReadyForPickup, RepairStatusCodes.InRepair],

        [RepairStatusCodes.ReadyForPickup] = [RepairStatusCodes.Delivered],

        // Trạng thái kết thúc (terminal) — không có transition nào đi tiếp
        [RepairStatusCodes.Delivered] = [],
        [RepairStatusCodes.ClosedRejected] = []
    };

    public static bool CanTransition(string fromCode, string toCode) =>
        AllowedTransitions.TryGetValue(fromCode, out var allowedNext) && allowedNext.Contains(toCode);

    public static void EnsureCanTransition(string fromCode, string toCode)
    {
        if (!CanTransition(fromCode, toCode))
            throw new DomainException(
                $"Không thể chuyển trạng thái phiếu sửa chữa từ '{fromCode}' sang '{toCode}'.");
    }
}