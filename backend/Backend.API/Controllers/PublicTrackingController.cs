using RepairShop.Application.Modules.Tickets.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RepairShop.API.Controllers;

/// <summary>
/// Controller RIÊNG cho toàn bộ endpoint public — tách khỏi TicketsController có chủ đích:
/// đảm bảo không ai vô tình thêm 1 action mới vào đây rồi quên gắn [Authorize], vì
/// TicketsController mặc định [Authorize] ở class-level còn Controller này mặc định public.
/// Nhìn tên file/route là biết ngay đây là vùng "không cần đăng nhập" - giảm rủi ro lộ dữ liệu do nhầm lẫn.
/// </summary>
[ApiController]
[Route("api/public/tickets")]
[AllowAnonymous]
public class PublicTrackingController : ControllerBase
{
    private readonly IMediator _mediator;

    public PublicTrackingController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{ticketCode}/tracking")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("PublicTrackingPolicy")]
    public async Task<IActionResult> Track(string ticketCode)
    {
        var result = await _mediator.Send(new TrackTicketByCodeQuery(ticketCode));
        return Ok(result);
    }
}