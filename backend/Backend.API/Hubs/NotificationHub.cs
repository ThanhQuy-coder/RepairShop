using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace RepairShop.API.Hubs;

/// <summary>
/// NFR-028: Hub bắt buộc xác thực JWT, KHÔNG cho phép kết nối ẩn danh. Group theo UserId đảm bảo
/// mỗi client chỉ nhận được thông báo của chính mình — đúng BR-28 (ownership), không rò rỉ sang user khác.
/// [Authorize] ở cấp Hub đã chặn hoàn toàn anonymous connection trước khi vào bất kỳ method nào.
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId is not null)
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupNameForUser(userId.Value));

        // Group theo Role — có thể mở rộng sau cho thông báo broadcast tới cả 1 nhóm vai trò
        // (VD: cảnh báo tồn kho thấp gửi mọi Admin cùng lúc) mà không cần lặp qua từng userId.
        var role = Context.User?.FindFirstValue(ClaimTypes.Role);
        if (!string.IsNullOrEmpty(role))
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupNameForRole(role));

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (userId is not null)
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupNameForUser(userId.Value));

        await base.OnDisconnectedAsync(exception);
    }

    private Guid? GetUserId()
    {
        // Khớp đúng claim "sub" đã dùng khi sinh JWT (JwtTokenGenerator, Task 6 Tuần 3)
        var sub = Context.User?.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    public static string GroupNameForUser(Guid userId) => $"user:{userId}";
    public static string GroupNameForRole(string role) => $"role:{role}";
}