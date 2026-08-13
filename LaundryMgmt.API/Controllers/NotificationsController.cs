using LaundryMgmt.Application.Notifications.Commands.MarkNotificationRead;
using LaundryMgmt.Application.Notifications.Queries.GetMyNotifications;
using LaundryMgmt.Application.Notifications.Queries.GetUnreadNotificationCount;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaundryMgmt.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly ISender _sender;

    public NotificationsController(ISender sender) => _sender = sender;

    /// <summary>The caller's notification feed — a shared inbox for Admin/StoreManager/
    /// Staff, or the caller's own notifications for any other role.</summary>
    [HttpGet("mine")]
    [ProducesResponseType(typeof(List<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<NotificationDto>>> GetMine([FromQuery] int take = 30)
    {
        var result = await _sender.Send(new GetMyNotificationsQuery(take));
        return Ok(result);
    }

    [HttpGet("mine/unread-count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<ActionResult<int>> GetUnreadCount()
    {
        var result = await _sender.Send(new GetUnreadNotificationCountQuery());
        return Ok(result);
    }

    [HttpPut("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        await _sender.Send(new MarkNotificationReadCommand(id));
        return NoContent();
    }
}
