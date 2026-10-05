using RepairShop.Application.Modules.Notifications.Commands;
using RepairShop.Application.Modules.Notifications.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RepairShop.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize] // bất kỳ role nào đã đăng nhập — ownership thật sự nằm trong Handler/Guard, không ở Policy này
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;
    public NotificationsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetMyNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 20) =>
        Ok(await _mediator.Send(new GetMyNotificationsQuery(page, pageSize)));

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount() => Ok(await _mediator.Send(new GetUnreadCountQuery()));

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        await _mediator.Send(new MarkNotificationReadCommand(id));
        return NoContent();
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        await _mediator.Send(new MarkAllNotificationsReadCommand());
        return NoContent();
    }
}