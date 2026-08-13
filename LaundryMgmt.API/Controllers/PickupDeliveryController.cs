using LaundryMgmt.Application.Common.Constants;
using LaundryMgmt.Application.Common.Models;
using LaundryMgmt.Application.PickupDeliveries.Commands.AssignAgent;
using LaundryMgmt.Application.PickupDeliveries.Commands.ConfirmDelivery;
using LaundryMgmt.Application.PickupDeliveries.Commands.ConfirmPickup;
using LaundryMgmt.Application.PickupDeliveries.Queries.GetAgents;
using LaundryMgmt.Application.PickupDeliveries.Queries.GetMyPickupDeliveries;
using LaundryMgmt.Application.PickupDeliveries.Queries.GetOrderPickupDeliveries;
using LaundryMgmt.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaundryMgmt.API.Controllers;

[ApiController]
[Route("api/v1/pickup-delivery")]
[Authorize]
public class PickupDeliveryController : ControllerBase
{
    private readonly ISender _sender;

    public PickupDeliveryController(ISender sender) => _sender = sender;

    /// <summary>The logged-in Pickup/Delivery Agent's own assigned orders.</summary>
    [HttpGet("mine")]
    [Authorize(Roles = "PickupAgent,DeliveryAgent")]
    [ProducesResponseType(typeof(PaginatedList<MyPickupDeliveryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedList<MyPickupDeliveryDto>>> GetMine(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _sender.Send(new GetMyPickupDeliveriesQuery(pageNumber, pageSize));
        return Ok(result);
    }

    /// <summary>Confirms garments received for a pickup assigned to the caller.
    /// Requires at least one pickup image already uploaded.</summary>
    [HttpPost("{id:guid}/confirm-pickup")]
    [Authorize(Roles = "PickupAgent")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ConfirmPickup(Guid id)
    {
        await _sender.Send(new ConfirmPickupCommand(id));
        return NoContent();
    }

    /// <summary>Confirms delivery for a delivery assigned to the caller. Requires at
    /// least one delivery-proof image already uploaded.</summary>
    [HttpPost("{id:guid}/confirm-delivery")]
    [Authorize(Roles = "DeliveryAgent")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ConfirmDelivery(Guid id)
    {
        await _sender.Send(new ConfirmDeliveryCommand(id));
        return NoContent();
    }

    /// <summary>The pickup/delivery legs for one order — for the order-detail screen.</summary>
    [HttpGet("order/{orderId:guid}")]
    [Authorize(Roles = AppRoles.OperationalViewRoles)]
    [ProducesResponseType(typeof(List<OrderPickupDeliveryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<OrderPickupDeliveryDto>>> GetForOrder(Guid orderId)
    {
        var result = await _sender.Send(new GetOrderPickupDeliveriesQuery(orderId));
        return Ok(result);
    }

    /// <summary>Active Pickup/Delivery Agent employees, for the assign-agent dropdown.</summary>
    [HttpGet("agents")]
    [Authorize(Roles = AppRoles.ManagementRoles)]
    [ProducesResponseType(typeof(List<AgentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AgentDto>>> GetAgents([FromQuery] UserRole role)
    {
        var result = await _sender.Send(new GetAgentsQuery(role));
        return Ok(result);
    }

    /// <summary>Assigns a Pickup/Delivery Agent employee to a pickup or delivery leg.</summary>
    [HttpPut("{id:guid}/assign")]
    [Authorize(Roles = AppRoles.ManagementRoles)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AssignAgent(Guid id, [FromBody] AssignAgentBody body)
    {
        await _sender.Send(new AssignAgentCommand(id, body.EmployeeId));
        return NoContent();
    }
}

public record AssignAgentBody(Guid EmployeeId);
