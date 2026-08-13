using LaundryMgmt.Application.Common.Constants;
using LaundryMgmt.Application.Common.Models;
using LaundryMgmt.Application.CustomerSubscriptions.Commands.AssignCustomerSubscription;
using LaundryMgmt.Application.CustomerSubscriptions.Commands.UpdateCustomerSubscription;
using LaundryMgmt.Application.CustomerSubscriptions.Queries.GetCustomerSubscriptionById;
using LaundryMgmt.Application.CustomerSubscriptions.Queries.GetCustomerSubscriptions;
using LaundryMgmt.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaundryMgmt.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = AppRoles.ManagementRoles)]
public class CustomerSubscriptionsController : ControllerBase
{
    private readonly ISender _sender;

    public CustomerSubscriptionsController(ISender sender) => _sender = sender;

    /// <summary>List customer subscriptions with optional search by customer name, paginated.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<CustomerSubscriptionListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedList<CustomerSubscriptionListItemDto>>> GetCustomerSubscriptions(
        [FromQuery] string? search, [FromQuery] Guid? customerId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _sender.Send(new GetCustomerSubscriptionsQuery(search, customerId, pageNumber, pageSize));
        return Ok(result);
    }

    /// <summary>Get a single customer subscription.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CustomerSubscriptionDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerSubscriptionDetailDto>> GetCustomerSubscriptionById(Guid id)
    {
        var result = await _sender.Send(new GetCustomerSubscriptionByIdQuery(id));
        return Ok(result);
    }

    /// <summary>Put a customer on a subscription plan with an admin-set recurring value.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> AssignSubscription(AssignCustomerSubscriptionCommand command)
    {
        var subscriptionId = await _sender.Send(command);
        return CreatedAtAction(nameof(GetCustomerSubscriptionById), new { id = subscriptionId }, subscriptionId);
    }

    /// <summary>Update a customer subscription's recurring value, status, or notes.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateSubscription(Guid id, [FromBody] UpdateCustomerSubscriptionBody body)
    {
        await _sender.Send(new UpdateCustomerSubscriptionCommand(
            id, body.RecurringValue, body.Status, body.NextBillingDate, body.Notes));
        return NoContent();
    }
}

public record UpdateCustomerSubscriptionBody(
    decimal RecurringValue, SubscriptionStatus Status, DateOnly? NextBillingDate, string? Notes);
