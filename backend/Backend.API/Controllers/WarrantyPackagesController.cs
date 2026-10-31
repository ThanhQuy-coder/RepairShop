using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepairShop.Application.Modules.WarrantyPackage;
using RepairShop.Infrastructure.Identity;

namespace RepairShop.API.Controllers;

[ApiController]
[Route("api/warranty-packages")]
public class WarrantyPackagesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetActive() =>
        Ok(await mediator.Send(new GetWarrantyPackagesQuery(true)));

    [HttpGet("admin")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> GetAll() =>
        Ok(await mediator.Send(new GetWarrantyPackagesQuery(null)));

    public record PackageBody(string Name, string? Description, int DurationMonths, string Terms);

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Create(PackageBody body) =>
        Ok(await mediator.Send(new CreateWarrantyPackageCommand(body.Name, body.Description, body.DurationMonths, body.Terms)));

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Update(Guid id, PackageBody body) =>
        Ok(await mediator.Send(new UpdateWarrantyPackageCommand(id, body.Name, body.Description, body.DurationMonths, body.Terms)));

    public record ToggleBody(bool IsActive);

    [HttpPatch("{id:guid}/active")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Toggle(Guid id, ToggleBody body)
    {
        await mediator.Send(new ToggleWarrantyPackageCommand(id, body.IsActive));
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await mediator.Send(new DeleteWarrantyPackageCommand(id));
        return NoContent();
    }
}
