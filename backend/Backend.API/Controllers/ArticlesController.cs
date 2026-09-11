using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Modules.Content.Commands;
using RepairShop.Application.Modules.Content.Queries;
using RepairShop.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepairShop.Domain.Common;

namespace RepairShop.API.Controllers;

[ApiController]
[Route("api/articles")]
public class ArticlesController : ControllerBase
{
    private readonly IMediator _mediator;
    public ArticlesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublished([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetArticlesQuery(true, page, pageSize));
        return Ok(result);
    }

    [HttpGet("admin")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetArticlesQuery(null, page, pageSize));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetArticleByIdQuery(id));

        // Public: chỉ xem được bài đã publish. Admin gọi qua route riêng nên luôn xem được.
        var isAdminRequest = User.IsInRole(Roles.Admin);
        if (!result.IsPublished && !isAdminRequest)
            return NotFound(new { success = false, message = "Không tìm thấy bài viết." });

        return Ok(result);
    }

    public record CreateArticleBody(string Title, string Content, string? ImageUrl);

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Create(CreateArticleBody body)
    {
        var result = await _mediator.Send(new CreateArticleCommand(body.Title, body.Content, body.ImageUrl));
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Update(Guid id, CreateArticleBody body)
    {
        var result = await _mediator.Send(new UpdateArticleCommand(id, body.Title, body.Content, body.ImageUrl));
        return Ok(result);
    }

    public record TogglePublishBody(bool Publish);

    [HttpPatch("{id:guid}/publish")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> TogglePublish(Guid id, TogglePublishBody body)
    {
        await _mediator.Send(new ToggleArticlePublishCommand(id, body.Publish));
        return NoContent();
    }
}