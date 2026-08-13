using LaundryMgmt.Application.AddOns.Commands.CreateAddOn;
using LaundryMgmt.Application.AddOns.Commands.DeleteAddOn;
using LaundryMgmt.Application.AddOns.Commands.UpdateAddOn;
using LaundryMgmt.Application.AddOns.Queries.GetAddOns;
using LaundryMgmt.Application.Common.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaundryMgmt.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class AddOnsController : ControllerBase
{
    private readonly ISender _sender;

    public AddOnsController(ISender sender) => _sender = sender;

    /// <summary>List add-ons (Stain Removal, Premium Packaging, ...), optionally filtered to active-only.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<AddOnDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AddOnDto>>> GetAddOns([FromQuery] bool? isActive = null)
    {
        var result = await _sender.Send(new GetAddOnsQuery(isActive));
        return Ok(result);
    }

    /// <summary>Add a new add-on.</summary>
    [HttpPost]
    [Authorize(Roles = AppRoles.ManagementRoles)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> CreateAddOn(CreateAddOnCommand command)
    {
        var addOnId = await _sender.Send(command);
        return CreatedAtAction(nameof(GetAddOns), new { id = addOnId }, addOnId);
    }

    /// <summary>Update an add-on's details.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = AppRoles.ManagementRoles)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateAddOn(Guid id, [FromBody] UpdateAddOnBody body)
    {
        await _sender.Send(new UpdateAddOnCommand(id, body.Name, body.Description, body.Price, body.IsActive));
        return NoContent();
    }

    /// <summary>Remove an add-on from the catalog.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = AppRoles.ManagementRoles)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAddOn(Guid id, [FromQuery] string? reason)
    {
        await _sender.Send(new DeleteAddOnCommand(id, reason));
        return NoContent();
    }
}

public record UpdateAddOnBody(string Name, string? Description, decimal Price, bool IsActive);
