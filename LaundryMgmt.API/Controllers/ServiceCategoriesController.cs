using LaundryMgmt.Application.Common.Constants;
using LaundryMgmt.Application.ServiceCategories.Commands.CreateServiceCategory;
using LaundryMgmt.Application.ServiceCategories.Commands.DeleteServiceCategory;
using LaundryMgmt.Application.ServiceCategories.Commands.UpdateServiceCategory;
using LaundryMgmt.Application.ServiceCategories.Queries.GetServiceCategories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaundryMgmt.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ServiceCategoriesController : ControllerBase
{
    private readonly ISender _sender;

    public ServiceCategoriesController(ISender sender) => _sender = sender;

    /// <summary>List all service categories (top-level groupings shown to customers).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<ServiceCategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ServiceCategoryDto>>> GetServiceCategories()
    {
        var result = await _sender.Send(new GetServiceCategoriesQuery());
        return Ok(result);
    }

    /// <summary>Add a new service category.</summary>
    [HttpPost]
    [Authorize(Roles = AppRoles.ManagementRoles)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> CreateServiceCategory(CreateServiceCategoryCommand command)
    {
        var categoryId = await _sender.Send(command);
        return CreatedAtAction(nameof(GetServiceCategories), new { id = categoryId }, categoryId);
    }

    /// <summary>Update a service category's details.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = AppRoles.ManagementRoles)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateServiceCategory(Guid id, [FromBody] UpdateServiceCategoryBody body)
    {
        await _sender.Send(new UpdateServiceCategoryCommand(
            id, body.Name, body.Description, body.IconOrImageUrl, body.DisplayOrder, body.IsActive));
        return NoContent();
    }

    /// <summary>Remove a service category (blocked while any service still belongs to it).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = AppRoles.ManagementRoles)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteServiceCategory(Guid id, [FromQuery] string? reason)
    {
        await _sender.Send(new DeleteServiceCategoryCommand(id, reason));
        return NoContent();
    }
}

public record UpdateServiceCategoryBody(string Name, string? Description, string? IconOrImageUrl, int DisplayOrder, bool IsActive);
