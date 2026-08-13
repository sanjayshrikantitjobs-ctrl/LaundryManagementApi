using System.Linq.Expressions;
using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Application.Common.Models;
using LaundryMgmt.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Infrastructure.Identity;

public class UserManagementService : IUserManagementService
{
    private static readonly Dictionary<string, Expression<Func<ApplicationUser, object>>> SortableColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["fullName"] = u => u.FullName,
        ["userName"] = u => u.UserName!,
        ["role"] = u => u.Role,
        ["isActive"] = u => u.IsActive
    };

    private readonly UserManager<ApplicationUser> _userManager;

    public UserManagementService(UserManager<ApplicationUser> userManager) => _userManager = userManager;

    public async Task<(List<UserSummaryDto> Items, int TotalCount)> GetUsersAsync(
        string? search, UserRole? role, bool? isActive, int pageNumber, int pageSize,
        string? sortBy, string? sortDirection, CancellationToken cancellationToken = default)
    {
        // Customer accounts (created via self-registration) live in the same Identity
        // store as staff accounts but belong exclusively to the Customers screen —
        // exclude them here unconditionally so they can never leak into the admin
        // Users list regardless of the caller's optional role filter.
        var query = _userManager.Users.Where(u => u.Role != UserRole.Customer).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u =>
                u.FullName.Contains(search) ||
                (u.UserName != null && u.UserName.Contains(search)) ||
                (u.Email != null && u.Email.Contains(search)));

        if (role.HasValue)
            query = query.Where(u => u.Role == role.Value);

        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .ApplySort(sortBy, sortDirection, SortableColumns, u => u.FullName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserSummaryDto(u.Id, u.UserName ?? string.Empty, u.Email, u.PhoneNumber, u.FullName, u.Role, u.IsActive))
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<UserSummaryDto?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        return user is null ? null : ToDto(user);
    }

    public async Task<Guid> CreateUserAsync(
        string fullName, string userName, string? email, string? phoneNumber,
        string password, UserRole role, CancellationToken cancellationToken = default)
    {
        if (await _userManager.FindByNameAsync(userName) is not null)
            throw new InvalidOperationException($"An account with username {userName} already exists.");

        var user = new ApplicationUser
        {
            UserName = userName,
            Email = email,
            EmailConfirmed = email is not null,
            PhoneNumber = phoneNumber,
            PhoneNumberConfirmed = phoneNumber is not null,
            FullName = fullName,
            Role = role,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));

        await _userManager.AddToRoleAsync(user, role.ToString());
        return user.Id;
    }

    public async Task UpdateUserAsync(Guid id, string fullName, string? email, string? phoneNumber, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString())
            ?? throw new KeyNotFoundException($"User {id} not found.");

        user.FullName = fullName;
        user.Email = email;
        user.PhoneNumber = phoneNumber;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    public async Task SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString())
            ?? throw new KeyNotFoundException($"User {id} not found.");

        user.IsActive = isActive;
        await _userManager.UpdateAsync(user);
    }

    public async Task AssignRoleAsync(Guid id, UserRole role, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString())
            ?? throw new KeyNotFoundException($"User {id} not found.");

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Count > 0)
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

        user.Role = role;
        await _userManager.UpdateAsync(user);
        await _userManager.AddToRoleAsync(user, role.ToString());
    }

    private static UserSummaryDto ToDto(ApplicationUser user) =>
        new(user.Id, user.UserName ?? string.Empty, user.Email, user.PhoneNumber, user.FullName, user.Role, user.IsActive);
}
