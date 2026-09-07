using RepairShop.Application.Modules.AIAdvisory.Commands.GetAIAdvice;
using RepairShop.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RepairShop.API.Controllers;

[ApiController]
[Route("api/ai")]
public class AIAdvisoryController : ControllerBase
{
    private readonly IMediator _mediator;
    public AIAdvisoryController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Contract Task 4 mục 13.1 Tuần 2: POST /api/ai/advice — Authorization: Customer, Receptionist.
    /// Request chỉ chứa deviceType/brand/model/issueDescription — Backend TỰ query context
    /// thật từ PostgreSQL ở tầng Infrastructure (Task 6.15), Frontend không gửi giá/dịch vụ nào cả.
    /// </summary>
    [HttpPost("advice")]
    [Authorize(Policy = AuthorizationPolicies.CustomerOrReceptionist)]
    public async Task<IActionResult> GetAdvice(GetAIAdviceCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }
}