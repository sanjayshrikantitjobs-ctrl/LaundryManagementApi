using LaundryMgmt.Application.Common.Constants;
using LaundryMgmt.Application.Common.Models;
using LaundryMgmt.Application.ContactMessages.Commands.CreateContactMessage;
using LaundryMgmt.Application.ContactMessages.Commands.RespondToContactMessage;
using LaundryMgmt.Application.ContactMessages.Queries.GetContactMessages;
using LaundryMgmt.Application.ContactMessages.Queries.GetMyContactMessages;
using LaundryMgmt.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaundryMgmt.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ContactMessagesController : ControllerBase
{
    private readonly ISender _sender;

    public ContactMessagesController(ISender sender) => _sender = sender;

    /// <summary>Submit a Contact Us message (feedback, complaint, or query), with an optional
    /// image uploaded first via <c>/api/v1/uploads/images</c>.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Create(CreateContactMessageCommand command)
    {
        var id = await _sender.Send(command);
        return CreatedAtAction(nameof(GetMine), new { }, id);
    }

    /// <summary>The logged-in customer's own submitted Contact Us messages and their status.</summary>
    [HttpGet("mine")]
    [ProducesResponseType(typeof(List<MyContactMessageDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<MyContactMessageDto>>> GetMine()
    {
        var result = await _sender.Send(new GetMyContactMessagesQuery());
        return Ok(result);
    }

    /// <summary>Admin/staff inbox for all customer-submitted messages.</summary>
    [HttpGet]
    [Authorize(Roles = AppRoles.ManagementRoles)]
    [ProducesResponseType(typeof(PaginatedList<ContactMessageListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedList<ContactMessageListItemDto>>> GetAll(
        [FromQuery] ContactMessageStatus? status, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null, [FromQuery] string? sortDirection = null)
    {
        var result = await _sender.Send(new GetContactMessagesQuery(status, pageNumber, pageSize, sortBy, sortDirection));
        return Ok(result);
    }

    /// <summary>Reply to a customer's message and mark it resolved.</summary>
    [HttpPut("{id:guid}/respond")]
    [Authorize(Roles = AppRoles.ManagementRoles)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Respond(Guid id, [FromBody] RespondBody body)
    {
        await _sender.Send(new RespondToContactMessageCommand(id, body.Response));
        return NoContent();
    }
}

public record RespondBody(string Response);
