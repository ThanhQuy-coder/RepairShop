using System.Globalization;
using System.Text.Json;
using RepairShop.Application.Modules.Content.Commands;
using RepairShop.Application.Modules.Content.Queries;
using RepairShop.Application.Modules.Reviews.Queries;
using RepairShop.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RepairShop.API.Controllers;

[ApiController]
[Route("api/services")]
public class ServicesController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IMediator _mediator;
    public ServicesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [AllowAnonymous] // FR-049: Customer xem công khai, chỉ thấy isActive=true
    public async Task<IActionResult> GetPublicServices(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? deviceType = null,
        [FromQuery] string? search = null)
    {
        var result = await _mediator.Send(new GetServicesQuery(true, page, pageSize, deviceType, search));
        return Ok(result);
    }

    [HttpGet("admin")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)] // Admin xem cả bản Unpublish
    public async Task<IActionResult> GetAllServices(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? deviceType = null,
        [FromQuery] string? search = null)
    {
        var result = await _mediator.Send(new GetServicesQuery(isActive, page, pageSize, deviceType, search));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetServiceByIdQuery(id));
        return Ok(result);
    }

    [HttpGet("{id:guid}/reviews")]
    [AllowAnonymous]
    public async Task<IActionResult> GetServiceReviews(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _mediator.Send(new GetServiceReviewsQuery(id, page, pageSize));
        return Ok(result);
    }

    public record CreateServiceBody(
        string Name,
        string? Description,
        decimal? BasePrice,
        string? DeviceType,
        string? ImageUrl = null,
        bool RemoveImage = false);

    [HttpPost("upload-image")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadImage(IFormFile? file)
    {
        var uploadedFile = file ?? Request.Form.Files.FirstOrDefault();
        if (uploadedFile is null)
            return BadRequest(new { success = false, message = "File ảnh không được để trống." });

        await using var stream = uploadedFile.OpenReadStream();
        var result = await _mediator.Send(new UploadServiceImageCommand(
            stream,
            uploadedFile.FileName,
            uploadedFile.ContentType,
            uploadedFile.Length));
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> Create()
    {
        if (Request.HasFormContentType)
        {
            var form = await Request.ReadFormAsync();
            var name = form["name"].FirstOrDefault() ?? string.Empty;
            var description = form["description"].FirstOrDefault();
            var deviceType = form["deviceType"].FirstOrDefault();
            var imageUrl = form["imageUrl"].FirstOrDefault();
            decimal? basePrice = null;
            var rawPrice = form["basePrice"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(rawPrice) &&
                decimal.TryParse(rawPrice, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedPrice))
            {
                basePrice = parsedPrice;
            }

            var file = form.Files.GetFile("image") ?? form.Files.GetFile("file") ?? form.Files.FirstOrDefault();
            if (file is not null)
            {
                await using var stream = file.OpenReadStream();
                var createdWithFile = await _mediator.Send(new CreateServiceCommand(
                    name,
                    description,
                    basePrice,
                    deviceType,
                    imageUrl,
                    stream,
                    file.FileName,
                    file.ContentType,
                    file.Length));
                return Ok(createdWithFile);
            }

            var createdFromForm = await _mediator.Send(new CreateServiceCommand(
                name,
                description,
                basePrice,
                deviceType,
                imageUrl));
            return Ok(createdFromForm);
        }

        var body = await JsonSerializer.DeserializeAsync<CreateServiceBody>(Request.Body, JsonOptions);
        if (body is null)
            return BadRequest(new { success = false, message = "Dữ liệu yêu cầu không hợp lệ." });

        var result = await _mediator.Send(new CreateServiceCommand(
            body.Name,
            body.Description,
            body.BasePrice,
            body.DeviceType,
            body.ImageUrl));
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> Update(Guid id)
    {
        if (Request.HasFormContentType)
        {
            var form = await Request.ReadFormAsync();
            var name = form["name"].FirstOrDefault() ?? string.Empty;
            var description = form["description"].FirstOrDefault();
            var deviceType = form["deviceType"].FirstOrDefault();
            var imageUrl = form.ContainsKey("imageUrl") ? form["imageUrl"].FirstOrDefault() : null;
            var removeImage = string.Equals(form["removeImage"].FirstOrDefault(), "true", StringComparison.OrdinalIgnoreCase);

            decimal? basePrice = null;
            var rawPrice = form["basePrice"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(rawPrice) &&
                decimal.TryParse(rawPrice, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedPrice))
            {
                basePrice = parsedPrice;
            }

            var file = form.Files.GetFile("image") ?? form.Files.GetFile("file") ?? form.Files.FirstOrDefault();
            if (file is not null)
            {
                await using var stream = file.OpenReadStream();
                var updatedWithFile = await _mediator.Send(new UpdateServiceCommand(
                    id,
                    name,
                    description,
                    basePrice,
                    deviceType,
                    imageUrl,
                    stream,
                    file.FileName,
                    file.ContentType,
                    file.Length,
                    removeImage));
                return Ok(updatedWithFile);
            }

            var updatedFromForm = await _mediator.Send(new UpdateServiceCommand(
                id,
                name,
                description,
                basePrice,
                deviceType,
                imageUrl,
                RemoveImage: removeImage));
            return Ok(updatedFromForm);
        }

        var body = await JsonSerializer.DeserializeAsync<CreateServiceBody>(Request.Body, JsonOptions);
        if (body is null)
            return BadRequest(new { success = false, message = "Dữ liệu yêu cầu không hợp lệ." });

        var result = await _mediator.Send(new UpdateServiceCommand(
            id,
            body.Name,
            body.Description,
            body.BasePrice,
            body.DeviceType,
            body.ImageUrl,
            RemoveImage: body.RemoveImage));
        return Ok(result);
    }

    public record TogglePublishBody(bool Publish);

    [HttpPatch("{id:guid}/publish")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> TogglePublish(Guid id, TogglePublishBody body)
    {
        await _mediator.Send(new ToggleServicePublishCommand(id, body.Publish));
        return NoContent();
    }
}
