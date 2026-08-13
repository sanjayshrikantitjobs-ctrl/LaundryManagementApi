using LaundryMgmt.Application.Common.Constants;
using LaundryMgmt.Application.Common.Models;
using LaundryMgmt.Application.Customers.Commands.AddCustomerAddress;
using LaundryMgmt.Application.Customers.Commands.AddMyAddress;
using LaundryMgmt.Application.Customers.Commands.AdminSetCustomerPassword;
using LaundryMgmt.Application.Customers.Commands.CreateCustomer;
using LaundryMgmt.Application.Customers.Commands.DeleteCustomer;
using LaundryMgmt.Application.Customers.Commands.DeleteMyAddress;
using LaundryMgmt.Application.Customers.Commands.SetPrimaryAddress;
using LaundryMgmt.Application.Customers.Commands.UpdateCustomer;
using LaundryMgmt.Application.Customers.Queries.GetCustomerById;
using LaundryMgmt.Application.Customers.Queries.GetCustomers;
using LaundryMgmt.Application.Customers.Queries.GetMyAddresses;
using LaundryMgmt.Application.Customers.Commands.UpdateMyProfile;
using LaundryMgmt.Application.Customers.Commands.SetCustomerStatus;
using LaundryMgmt.Application.Customers.Queries.GetMyCustomerProfile;
using LaundryMgmt.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaundryMgmt.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly ISender _sender;

    public CustomersController(ISender sender) => _sender = sender;

    /// <summary>List customers with optional search, paginated. Staff/admin/department-head
    /// only — customers have no legitimate reason to browse other customers' records.</summary>
    [HttpGet]
    [Authorize(Roles = AppRoles.OperationalViewRoles)]
    [ProducesResponseType(typeof(PaginatedList<CustomerListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedList<CustomerListItemDto>>> GetCustomers(
        [FromQuery] string? search, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null, [FromQuery] string? sortDirection = null,
        [FromQuery] bool? hasOrders = null, [FromQuery] CustomerStatus? status = null)
    {
        var result = await _sender.Send(new GetCustomersQuery(search, pageNumber, pageSize, sortBy, sortDirection, hasOrders, status));
        return Ok(result);
    }

    /// <summary>Get a single customer with their addresses.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = AppRoles.OperationalViewRoles)]
    [ProducesResponseType(typeof(CustomerDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerDetailDto>> GetCustomerById(Guid id)
    {
        var result = await _sender.Send(new GetCustomerByIdQuery(id));
        return Ok(result);
    }

    /// <summary>The logged-in customer's own profile — used by the mobile/web
    /// customer portal to place an order under itself without browsing other records.</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(CustomerListItemDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerListItemDto>> GetMyProfile()
    {
        var result = await _sender.Send(new GetMyCustomerProfileQuery());
        return Ok(result);
    }

    /// <summary>Update the logged-in customer's own profile (Settings &gt; General tab).</summary>
    [HttpPut("me")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateMyProfile(UpdateMyProfileCommand command)
    {
        await _sender.Send(command);
        return NoContent();
    }

    /// <summary>Register a new customer, optionally with initial addresses.</summary>
    [HttpPost]
    [Authorize(Roles = AppRoles.ManagementRoles)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> CreateCustomer(CreateCustomerCommand command)
    {
        var customerId = await _sender.Send(command);
        return CreatedAtAction(nameof(GetCustomerById), new { id = customerId }, customerId);
    }

    /// <summary>Update a customer's profile.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = AppRoles.ManagementRoles)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateCustomer(Guid id, [FromBody] UpdateCustomerBody body)
    {
        await _sender.Send(new UpdateCustomerCommand(
            id, body.FullName, body.PhoneNumber, body.Email, body.CreditLimit, body.Notes));
        return NoContent();
    }

    /// <summary>Soft-delete a customer.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = AppRoles.ManagementRoles)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteCustomer(Guid id, [FromQuery] string? reason)
    {
        await _sender.Send(new DeleteCustomerCommand(id, reason));
        return NoContent();
    }

    /// <summary>Activate or deactivate a customer's account. Deactivating never
    /// deletes or hides their historical orders, addresses or invoices — it only
    /// blocks their portal login and flags the account for staff.</summary>
    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = AppRoles.ManagementRoles)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] SetCustomerStatusBody body)
    {
        await _sender.Send(new SetCustomerStatusCommand(id, body.Status));
        return NoContent();
    }

    /// <summary>Set a customer's portal login password (admin/staff only) — for
    /// resetting a customer's access without needing their old password.</summary>
    [HttpPut("{id:guid}/password")]
    [Authorize(Roles = AppRoles.ManagementRoles)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetPassword(Guid id, [FromBody] SetPasswordBody body)
    {
        await _sender.Send(new AdminSetCustomerPasswordCommand(id, body.NewPassword));
        return NoContent();
    }

    /// <summary>Add an address to an existing customer.</summary>
    [HttpPost("{id:guid}/addresses")]
    [Authorize(Roles = AppRoles.ManagementRoles)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> AddAddress(Guid id, [FromBody] AddAddressBody body)
    {
        var addressId = await _sender.Send(new AddCustomerAddressCommand(
            id, body.Label, body.Line1, body.Line2, body.City, body.State, body.PostalCode, body.IsDefault));
        return CreatedAtAction(nameof(GetCustomerById), new { id }, addressId);
    }

    /// <summary>The logged-in customer's own address book, for the Settings &gt; Address tab.</summary>
    [HttpGet("me/addresses")]
    [ProducesResponseType(typeof(List<MyAddressDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<MyAddressDto>>> GetMyAddresses()
    {
        var result = await _sender.Send(new GetMyAddressesQuery());
        return Ok(result);
    }

    /// <summary>Add a new address to the logged-in customer's own address book.</summary>
    [HttpPost("me/addresses")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> AddMyAddress(AddAddressBody body)
    {
        var addressId = await _sender.Send(new AddMyAddressCommand(
            body.Label, body.Line1, body.Line2, body.City, body.State, body.PostalCode, body.IsDefault));
        return CreatedAtAction(nameof(GetMyAddresses), new { }, addressId);
    }

    /// <summary>Mark one of the logged-in customer's own addresses as the primary
    /// pickup/delivery address (clears the flag on any other address).</summary>
    [HttpPut("me/addresses/{addressId:guid}/primary")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetPrimaryAddress(Guid addressId)
    {
        await _sender.Send(new SetPrimaryAddressCommand(addressId));
        return NoContent();
    }

    /// <summary>Remove an address from the logged-in customer's own address book.</summary>
    [HttpDelete("me/addresses/{addressId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteMyAddress(Guid addressId)
    {
        await _sender.Send(new DeleteMyAddressCommand(addressId));
        return NoContent();
    }
}

public record UpdateCustomerBody(
    string FullName, string PhoneNumber, string? Email, decimal CreditLimit, string? Notes);

public record AddAddressBody(
    string Label, string Line1, string? Line2, string City, string State, string PostalCode, bool IsDefault);

public record SetPasswordBody(string NewPassword);

public record SetCustomerStatusBody(CustomerStatus Status);
