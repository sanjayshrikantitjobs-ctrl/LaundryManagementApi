using LaundryMgmt.Application.Common.Constants;
using LaundryMgmt.Application.Common.Models;
using LaundryMgmt.Application.Garments.Commands.CreateGarment;
using LaundryMgmt.Application.Garments.Commands.DeleteGarment;
using LaundryMgmt.Application.Garments.Commands.SetGarmentServicePrice;
using LaundryMgmt.Application.Garments.Commands.UpdateGarment;
using LaundryMgmt.Application.Garments.Queries.GetGarmentById;
using LaundryMgmt.Application.Garments.Queries.GetGarments;
using LaundryMgmt.Application.Garments.Queries.GetPricingMatrix;
using LaundryMgmt.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaundryMgmt.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class GarmentsController : ControllerBase
{
    private readonly ISender _sender;

    public GarmentsController(ISender sender) => _sender = sender;

    /// <summary>List garments with optional search, paginated.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<GarmentListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedList<GarmentListItemDto>>> GetGarments(
        [FromQuery] string? search, [FromQuery] Guid? categoryId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null, [FromQuery] string? sortDirection = null)
    {
        var result = await _sender.Send(new GetGarmentsQuery(search, categoryId, pageNumber, pageSize, sortBy, sortDirection));
        return Ok(result);
    }

    /// <summary>Get a garment with its per-service prices.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GarmentDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<GarmentDetailDto>> GetGarmentById(Guid id)
    {
        var result = await _sender.Send(new GetGarmentByIdQuery(id));
        return Ok(result);
    }

    /// <summary>Full Garment x Service pricing grid, for the pricing management screen.</summary>
    [HttpGet("pricing-matrix")]
    [ProducesResponseType(typeof(PricingMatrixDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PricingMatrixDto>> GetPricingMatrix()
    {
        var result = await _sender.Send(new GetPricingMatrixQuery());
        return Ok(result);
    }

    /// <summary>Add a new garment to the catalog.</summary>
    [HttpPost]
    [Authorize(Roles = AppRoles.ManagementRoles)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> CreateGarment(CreateGarmentCommand command)
    {
        var garmentId = await _sender.Send(command);
        return CreatedAtAction(nameof(GetGarmentById), new { id = garmentId }, garmentId);
    }

    /// <summary>Update a garment's catalog details.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = AppRoles.ManagementRoles)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateGarment(Guid id, [FromBody] UpdateGarmentBody body)
    {
        await _sender.Send(new UpdateGarmentCommand(id, body.Name, body.CategoryId, body.SpecialInstructions, body.ImageUrl));
        return NoContent();
    }

    /// <summary>Remove a garment from the catalog.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = AppRoles.ManagementRoles)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteGarment(Guid id, [FromQuery] string? reason)
    {
        await _sender.Send(new DeleteGarmentCommand(id, reason));
        return NoContent();
    }

    /// <summary>Set (create or update) the price for a garment + service combination.</summary>
    [HttpPut("{id:guid}/prices/{serviceId:guid}")]
    [Authorize(Roles = AppRoles.ManagementRoles)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<ActionResult<Guid>> SetPrice(Guid id, Guid serviceId, [FromBody] SetPriceBody body)
    {
        var priceId = await _sender.Send(new SetGarmentServicePriceCommand(
            id, serviceId, body.PricingType, body.Price,
            body.ExpressPrice, body.GstPercentage, body.EstimatedTimeHours, body.ExpressEtaHours, body.IsActive));
        return Ok(priceId);
    }
}

public record UpdateGarmentBody(string Name, Guid CategoryId, string? SpecialInstructions, string? ImageUrl);

public record SetPriceBody(
    PricingType PricingType, decimal Price,
    decimal? ExpressPrice = null, decimal? GstPercentage = null,
    int? EstimatedTimeHours = null, int? ExpressEtaHours = null, bool IsActive = true);
