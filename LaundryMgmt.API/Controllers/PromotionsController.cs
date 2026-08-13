using LaundryMgmt.Application.Common.Constants;
using LaundryMgmt.Application.Common.Models;
using LaundryMgmt.Application.Promotions.Commands.CreatePromotion;
using LaundryMgmt.Application.Promotions.Commands.DeletePromotion;
using LaundryMgmt.Application.Promotions.Commands.UpdatePromotion;
using LaundryMgmt.Application.Promotions.Queries.GetActivePromotions;
using LaundryMgmt.Application.Promotions.Queries.GetPromotions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaundryMgmt.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class PromotionsController : ControllerBase
{
    private readonly ISender _sender;

    public PromotionsController(ISender sender) => _sender = sender;

    /// <summary>Admin listing of all promotions (active, expired, and disabled), paginated.</summary>
    [HttpGet]
    [Authorize(Roles = AppRoles.OperationalViewRoles)]
    [ProducesResponseType(typeof(PaginatedList<PromotionListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedList<PromotionListItemDto>>> GetPromotions(
        [FromQuery] string? search, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null, [FromQuery] string? sortDirection = null)
    {
        var result = await _sender.Send(new GetPromotionsQuery(search, pageNumber, pageSize, sortBy, sortDirection));
        return Ok(result);
    }

    /// <summary>Customer/public-facing listing — only currently active, in-window promotions.</summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(List<ActivePromotionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ActivePromotionDto>>> GetActivePromotions()
    {
        var result = await _sender.Send(new GetActivePromotionsQuery());
        return Ok(result);
    }

    /// <summary>Create a promotion/offer (title, image, optional promo code + discount + validity window).</summary>
    [HttpPost]
    [Authorize(Roles = AppRoles.ManagementRoles)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> CreatePromotion(CreatePromotionCommand command)
    {
        var promotionId = await _sender.Send(command);
        return CreatedAtAction(nameof(GetPromotions), new { id = promotionId }, promotionId);
    }

    /// <summary>Update a promotion.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = AppRoles.ManagementRoles)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdatePromotion(Guid id, [FromBody] UpdatePromotionBody body)
    {
        await _sender.Send(new UpdatePromotionCommand(
            id, body.Title, body.Description, body.ImageUrl, body.Code,
            body.DiscountPercent, body.DiscountAmount, body.ValidFrom, body.ValidTo, body.IsActive));
        return NoContent();
    }

    /// <summary>Remove a promotion.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = AppRoles.ManagementRoles)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeletePromotion(Guid id, [FromQuery] string? reason)
    {
        await _sender.Send(new DeletePromotionCommand(id, reason));
        return NoContent();
    }
}

public record UpdatePromotionBody(
    string Title, string? Description, string? ImageUrl, string? Code,
    decimal? DiscountPercent, decimal? DiscountAmount,
    DateTimeOffset? ValidFrom, DateTimeOffset? ValidTo, bool IsActive);
