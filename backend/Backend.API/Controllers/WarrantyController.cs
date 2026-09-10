using RepairShop.Application.Modules.Warranty.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RepairShop.API.Controllers;

[ApiController]
[Route("api/warranty")]
public class WarrantyController : ControllerBase
{
    private readonly IMediator _mediator;
    public WarrantyController(IMediator mediator) => _mediator = mediator;

    [HttpGet("my")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> GetMyWarranties()
    {
        var result = await _mediator.Send(new GetMyWarrantiesQuery());
        return Ok(result);
    }
}