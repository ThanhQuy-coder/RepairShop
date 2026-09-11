using RepairShop.Application.Modules.Reviews.Commands;
using RepairShop.Application.Modules.Reviews.Queries;
using RepairShop.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RepairShop.API.Controllers;

[ApiController]
[Route("api/reviews")]
public class ReviewsController : ControllerBase
{
    private readonly IMediator _mediator;
    public ReviewsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [AllowAnonymous] // Public chỉ thấy visible=true
    public async Task<IActionResult> GetPublicReviews([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetReviewsQuery(true, page, pageSize));
        return Ok(result);
    }

    [HttpGet("admin")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> GetAllReviews([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetReviewsQuery(null, page, pageSize));
        return Ok(result);
    }

    public record ToggleVisibilityBody(bool IsVisible);

    [HttpPatch("{id:guid}/visibility")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> ToggleVisibility(Guid id, ToggleVisibilityBody body)
    {
        await _mediator.Send(new ToggleReviewVisibilityCommand(id, body.IsVisible));
        return NoContent();
    }
}