using LaundryMgmt.Application.Common.Constants;
using LaundryMgmt.Application.OrderGarmentImages.Commands.AddOrderGarmentImage;
using LaundryMgmt.Application.OrderGarmentImages.Commands.DeleteOrderGarmentImage;
using LaundryMgmt.Application.OrderGarmentImages.Queries.GetOrderGarmentImages;
using LaundryMgmt.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaundryMgmt.API.Controllers;

/// <summary>Garment photos taken during pickup or delivery, scoped to one order.
/// Authorization/ownership is enforced in the command/query handlers (never UI-only) —
/// see AddOrderGarmentImageCommandHandler/GetOrderGarmentImagesQueryHandler.</summary>
[ApiController]
[Route("api/v1/orders/{orderId:guid}/images")]
[Authorize]
public class OrderGarmentImagesController : ControllerBase
{
    private readonly ISender _sender;

    public OrderGarmentImagesController(ISender sender) => _sender = sender;

    [HttpGet]
    [ProducesResponseType(typeof(List<OrderGarmentImageDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<OrderGarmentImageDto>>> GetImages(Guid orderId)
    {
        var result = await _sender.Send(new GetOrderGarmentImagesQuery(orderId));
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.ImageUploadRoles)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> AddImage(Guid orderId, [FromBody] AddImageBody body)
    {
        var imageId = await _sender.Send(new AddOrderGarmentImageCommand(
            orderId, body.ImageType, body.ImageUrl, body.ServiceId, body.OrderItemId, body.Notes));
        return CreatedAtAction(nameof(GetImages), new { orderId }, imageId);
    }

    [HttpDelete("{imageId:guid}")]
    [Authorize(Roles = AppRoles.ImageUploadRoles)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteImage(Guid orderId, Guid imageId)
    {
        await _sender.Send(new DeleteOrderGarmentImageCommand(imageId));
        return NoContent();
    }
}

public record AddImageBody(GarmentImageType ImageType, string ImageUrl, Guid? ServiceId, Guid? OrderItemId, string? Notes);
