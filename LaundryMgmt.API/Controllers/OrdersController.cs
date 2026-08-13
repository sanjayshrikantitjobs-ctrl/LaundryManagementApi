using LaundryMgmt.Application.Common.Constants;
using LaundryMgmt.Application.Common.Models;
using LaundryMgmt.Application.Orders.Commands.AdvanceOrderStatus;
using LaundryMgmt.Application.Orders.Commands.CreateOrder;
using LaundryMgmt.Application.Orders.Queries.GetMyOrders;
using LaundryMgmt.Application.Orders.Queries.GetOrderById;
using LaundryMgmt.Application.Orders.Commands.UpdateOrder;
using LaundryMgmt.Application.Orders.Queries.GetOrders;
using LaundryMgmt.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaundryMgmt.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly ISender _sender;

    public OrdersController(ISender sender) => _sender = sender;

    /// <summary>List orders with optional status filter and search, paginated.
    /// Staff/admin/department-head only — customers use /mine for their own history.</summary>
    [HttpGet]
    [Authorize(Roles = AppRoles.OperationalViewRoles)]
    [ProducesResponseType(typeof(PaginatedList<OrderListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedList<OrderListItemDto>>> GetOrders(
        [FromQuery] OrderStatus? status, [FromQuery] string? search,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null, [FromQuery] string? sortDirection = null,
        [FromQuery] string? statuses = null)
    {
        var result = await _sender.Send(new GetOrdersQuery(status, search, pageNumber, pageSize, sortBy, sortDirection, ParseStatuses(statuses)));
        return Ok(result);
    }

    /// <summary>Order history for the logged-in customer only.</summary>
    [HttpGet("mine")]
    [ProducesResponseType(typeof(PaginatedList<OrderListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedList<OrderListItemDto>>> GetMyOrders(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null, [FromQuery] string? sortDirection = null,
        [FromQuery] OrderStatus? status = null, [FromQuery] string? statuses = null)
    {
        var result = await _sender.Send(new GetMyOrdersQuery(pageNumber, pageSize, sortBy, sortDirection, status, ParseStatuses(statuses)));
        return Ok(result);
    }

    private static List<OrderStatus>? ParseStatuses(string? csv) =>
        string.IsNullOrWhiteSpace(csv)
            ? null
            : csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => Enum.Parse<OrderStatus>(s, ignoreCase: true))
                .ToList();

    /// <summary>Full detail for a single order (items, garments, services, pickup/delivery
    /// timing). Customers may only fetch their own order; management roles may fetch any.</summary>
    [HttpGet("{orderId:guid}")]
    [ProducesResponseType(typeof(OrderDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrderDetailDto>> GetOrderById(Guid orderId)
    {
        var result = await _sender.Send(new GetOrderByIdQuery(orderId));
        return Ok(result);
    }

    /// <summary>Create a new order (walk-in, pickup request, or express).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> CreateOrder(CreateOrderCommand command)
    {
        var orderId = await _sender.Send(command);
        return CreatedAtAction(nameof(GetOrders), new { id = orderId }, orderId);
    }

    /// <summary>Advance an order to the next step in the pipeline (or Cancelled).
    /// Department Head may perform this operational action even though they can't
    /// touch payment fields (see UpdateOrder below).</summary>
    [HttpPatch("{orderId:guid}/status")]
    [Authorize(Roles = AppRoles.OperationalRoles)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AdvanceStatus(Guid orderId, [FromBody] OrderStatus newStatus)
    {
        await _sender.Send(new AdvanceOrderStatusCommand(orderId, newStatus));
        return NoContent();
    }

    /// <summary>Admin order management: set status to any target step, payment status,
    /// amount paid, and/or expected delivery time. Notifies the customer of what changed.
    /// Kept Management-only (not Department Head) since it edits payment/financial fields.</summary>
    [HttpPut("{orderId:guid}")]
    [Authorize(Roles = AppRoles.ManagementRoles)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateOrder(Guid orderId, [FromBody] UpdateOrderBody body)
    {
        await _sender.Send(new UpdateOrderCommand(orderId, body.Status, body.PaymentStatus, body.AmountPaid, body.PromisedByUtc));
        return NoContent();
    }
}

public record UpdateOrderBody(
    OrderStatus? Status, PaymentStatus? PaymentStatus, decimal? AmountPaid, DateTimeOffset? PromisedByUtc);
