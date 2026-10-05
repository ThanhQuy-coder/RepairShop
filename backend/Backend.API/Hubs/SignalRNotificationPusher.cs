using RepairShop.API.Hubs;
using RepairShop.Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace RepairShop.API.Hubs;

public class SignalRNotificationPusher : INotificationPusher
{
    private readonly IHubContext<NotificationHub> _hubContext;
    public SignalRNotificationPusher(IHubContext<NotificationHub> hubContext) => _hubContext = hubContext;

    public Task PushToUserAsync(Guid userId, object payload) =>
        _hubContext.Clients.Group(NotificationHub.GroupNameForUser(userId)).SendAsync("ReceiveNotification", payload);

    public Task PushToRoleAsync(string role, object payload) =>
        _hubContext.Clients.Group(NotificationHub.GroupNameForRole(role)).SendAsync("ReceiveNotification", payload);
}