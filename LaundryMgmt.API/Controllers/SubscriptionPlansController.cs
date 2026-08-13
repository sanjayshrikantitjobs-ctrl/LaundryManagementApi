using LaundryMgmt.Application.Common.Constants;
using LaundryMgmt.Application.SubscriptionPlans.Commands.CreateSubscriptionPlan;
using LaundryMgmt.Application.SubscriptionPlans.Commands.DeleteSubscriptionPlan;
using LaundryMgmt.Application.SubscriptionPlans.Commands.UpdateSubscriptionPlan;
using LaundryMgmt.Application.SubscriptionPlans.Queries.GetSubscriptionPlans;
using LaundryMgmt.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaundryMgmt.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class SubscriptionPlansController : ControllerBase
{
    private readonly ISender _sender;

    public SubscriptionPlansController(ISender sender) => _sender = sender;

    /// <summary>List all subscription/membership plans. Open to any authenticated role
    /// so customers can browse plans on the Membership page.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<SubscriptionPlanDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SubscriptionPlanDto>>> GetSubscriptionPlans()
    {
        var result = await _sender.Send(new GetSubscriptionPlansQuery());
        return Ok(result);
    }

    /// <summary>Add a new subscription plan.</summary>
    [HttpPost]
    [Authorize(Roles = AppRoles.ManagementRoles)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> CreateSubscriptionPlan(CreateSubscriptionPlanCommand command)
    {
        var planId = await _sender.Send(command);
        return CreatedAtAction(nameof(GetSubscriptionPlans), new { id = planId }, planId);
    }

    /// <summary>Update a subscription plan's details and feature list.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = AppRoles.ManagementRoles)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateSubscriptionPlan(Guid id, [FromBody] UpdateSubscriptionPlanBody body)
    {
        await _sender.Send(new UpdateSubscriptionPlanCommand(
            id, body.Name, body.Description, body.BillingCycle, body.GarmentsPerCycle,
            body.Price, body.DisplayOrder, body.IsActive, body.Features));
        return NoContent();
    }

    /// <summary>Remove a subscription plan (blocked while any customer is on it).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = AppRoles.ManagementRoles)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteSubscriptionPlan(Guid id, [FromQuery] string? reason)
    {
        await _sender.Send(new DeleteSubscriptionPlanCommand(id, reason));
        return NoContent();
    }
}

public record UpdateSubscriptionPlanBody(
    string Name, string? Description, BillingCycle BillingCycle, int GarmentsPerCycle,
    decimal Price, int DisplayOrder, bool IsActive, List<string> Features);
