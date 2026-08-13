using LaundryMgmt.Application.Common.Constants;
using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Application.Common.Models;
using LaundryMgmt.Application.Users.Commands.AssignRole;
using LaundryMgmt.Application.Users.Commands.CreateUser;
using LaundryMgmt.Application.Users.Commands.SetUserActive;
using LaundryMgmt.Application.Users.Commands.UpdateUser;
using LaundryMgmt.Application.Users.Queries.GetUserById;
using LaundryMgmt.Application.Users.Queries.GetUsers;
using LaundryMgmt.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaundryMgmt.API.Controllers;

/// <summary>Admin-only user/role management — create staff and agent logins, assign
/// roles, and activate/deactivate accounts. Customer accounts are out of scope here
/// (they self-register via /api/v1/auth/register).</summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = AppRoles.Admin)]
public class UsersController : ControllerBase
{
    private readonly ISender _sender;

    public UsersController(ISender sender) => _sender = sender;

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<UserSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedList<UserSummaryDto>>> GetUsers(
        [FromQuery] string? search, [FromQuery] UserRole? role, [FromQuery] bool? isActive,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null, [FromQuery] string? sortDirection = null)
    {
        var result = await _sender.Send(new GetUsersQuery(search, role, isActive, pageNumber, pageSize, sortBy, sortDirection));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserSummaryDto>> GetUserById(Guid id)
    {
        var result = await _sender.Send(new GetUserByIdQuery(id));
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> CreateUser(CreateUserCommand command)
    {
        var userId = await _sender.Send(command);
        return CreatedAtAction(nameof(GetUserById), new { id = userId }, userId);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserBody body)
    {
        await _sender.Send(new UpdateUserCommand(id, body.FullName, body.Email, body.PhoneNumber));
        return NoContent();
    }

    [HttpPut("{id:guid}/active")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetActive(Guid id, [FromBody] SetActiveBody body)
    {
        await _sender.Send(new SetUserActiveCommand(id, body.IsActive));
        return NoContent();
    }

    [HttpPut("{id:guid}/role")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AssignRole(Guid id, [FromBody] AssignRoleBody body)
    {
        await _sender.Send(new AssignRoleCommand(id, body.Role));
        return NoContent();
    }
}

public record UpdateUserBody(string FullName, string? Email, string? PhoneNumber);

public record SetActiveBody(bool IsActive);

public record AssignRoleBody(UserRole Role);
