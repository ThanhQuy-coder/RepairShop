using RepairShop.Domain.Common;
using RepairShop.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RepairShop.API.Controllers;

[ApiController]
[Route("api/tickets")]
[Authorize] // mặc định: chỉ cần đăng nhập, role cụ thể xét lại từng action bên dưới
public class TicketsController : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ReceptionistOrAdmin)] // FR-015: Receptionist tạo ticket
    public IActionResult CreateTicket() => Ok();

    [HttpPatch("{id}/diagnosis")]
    [Authorize(Roles = Roles.Technician)] // FR-024: chỉ Technician nhập chẩn đoán — dùng Roles attribute trực tiếp cũng được
    public IActionResult SubmitDiagnosis(Guid id) => Ok();

    [HttpGet("track/{ticketCode}")]
    [AllowAnonymous] // FR-029: Customer tra cứu KHÔNG cần đăng nhập — override [Authorize] ở class
    public IActionResult TrackByCode(string ticketCode) => Ok();

    [HttpGet("{id}")]
    [Authorize(Policy = AuthorizationPolicies.StaffOnly)] // Admin/Receptionist/Technician đều xem được (Customer thì KHÔNG qua route này)
    public IActionResult GetTicketDetail(Guid id) => Ok();
}