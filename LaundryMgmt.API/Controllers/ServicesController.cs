using LaundryMgmt.Application.Common.Constants;
using LaundryMgmt.Application.Common.Models;
using LaundryMgmt.Application.Services.Commands.CreateService;
using LaundryMgmt.Application.Services.Commands.DeleteService;
using LaundryMgmt.Application.Services.Commands.UpdateService;
using LaundryMgmt.Application.Services.Queries.GetServiceById;
using LaundryMgmt.Application.Services.Queries.GetServices;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaundryMgmt.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ServicesController : ControllerBase
{
    private readonly ISender _sender;

    public ServicesController(ISender sender) => _sender = sender;

    /// <summary>List services with optional search, paginated.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<ServiceListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedList<ServiceListItemDto>>> GetServices(
        [FromQuery] string? search, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null, [FromQuery] string? sortDirection = null)
    {
        var result = await _sender.Send(new GetServicesQuery(search, pageNumber, pageSize, sortBy, sortDirection));
        return Ok(result);
    }

    /// <summary>Get a single service.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ServiceDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ServiceDetailDto>> GetServiceById(Guid id)
    {
        var result = await _sender.Send(new GetServiceByIdQuery(id));
        return Ok(result);
    }

    /// <summary>Add a new service (Washing, Dry Cleaning, Iron Only, ...).</summary>
    [HttpPost]
    [Authorize(Roles = AppRoles.ManagementRoles)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> CreateService(CreateServiceCommand command)
    {
        var serviceId = await _sender.Send(command);
        return CreatedAtAction(nameof(GetServiceById), new { id = serviceId }, serviceId);
    }

    /// <summary>Update a service's pricing/timing details.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = AppRoles.ManagementRoles)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateService(Guid id, [FromBody] UpdateServiceBody body)
    {
        await _sender.Send(new UpdateServiceCommand(
            id, body.Name, body.CategoryId, body.BasePrice, body.EstimatedTimeHours, body.GstPercentage, body.Priority, body.ImageUrl,
            body.ExpressSurcharge, body.ExpressEtaHours));
        return NoContent();
    }

    /// <summary>Remove a service from the catalog.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = AppRoles.ManagementRoles)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteService(Guid id, [FromQuery] string? reason)
    {
        await _sender.Send(new DeleteServiceCommand(id, reason));
        return NoContent();
    }
}

public record UpdateServiceBody(
    string Name, Guid CategoryId, decimal BasePrice, int EstimatedTimeHours, decimal GstPercentage, int Priority, string? ImageUrl,
    decimal ExpressSurcharge, int ExpressEtaHours);
