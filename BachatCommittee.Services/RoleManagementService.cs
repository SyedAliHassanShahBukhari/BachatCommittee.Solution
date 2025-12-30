// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using awisk.common.Helpers;
using BachatCommittee.Models.Classes;
using BachatCommittee.Models.DTOs.Requests;
using BachatCommittee.Models.DTOs.Responses;
using BachatCommittee.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BachatCommittee.Services;

public class RoleManagementService(
    RoleManager<IdentityRole> roleManager,
    UserManager<AppUser> userManager,
    IMemoryCache cache) : IRoleManagementService
{
    private readonly RoleManager<IdentityRole> _roleManager = roleManager;
    private readonly UserManager<AppUser> _userManager = userManager;
    private readonly IMemoryCache _cache = cache;
    private const string AllRolesCacheKey = "AllRoles";
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(15);

    public async Task<List<RoleResponseDto>> GetAllRolesAsync(CancellationToken cancellationToken = default)
    {
        // Try to get from cache first
        if (_cache.TryGetValue(AllRolesCacheKey, out List<RoleResponseDto>? cachedRoles) && cachedRoles != null)
        {
            return cachedRoles;
        }

        var roles = await _roleManager.Roles.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        var result = new List<RoleResponseDto>(roles.Count);

        // Compute user counts sequentially to avoid concurrent DbContext operations
        foreach (var role in roles)
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!).ConfigureAwait(false);
            result.Add(MapToRoleResponseDto(role, usersInRole.Count));
        }

        // Cache the result
        _cache.Set(AllRolesCacheKey, result, _cacheExpiration);

        return result;
    }

    public async Task<RoleResponseDto?> GetRoleByIdAsync(string roleId, CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByIdAsync(roleId).ConfigureAwait(false);
        if (role == null)
        {
            return null;
        }

        var userCount = (await _userManager.GetUsersInRoleAsync(role.Name!).ConfigureAwait(false)).Count;
        return MapToRoleResponseDto(role, userCount);
    }

    public async Task<RoleResponseDto?> GetRoleByNameAsync(string roleName, CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByNameAsync(roleName).ConfigureAwait(false);
        if (role == null)
        {
            return null;
        }

        var userCount = (await _userManager.GetUsersInRoleAsync(role.Name!).ConfigureAwait(false)).Count;
        return MapToRoleResponseDto(role, userCount);
    }

    public async Task<RoleResponseDto> CreateRoleAsync(CreateRoleRequestDto model, CancellationToken cancellationToken = default)
    {
        var role = new IdentityRole(model.Name);
        var result = await _roleManager.CreateAsync(role).ConfigureAwait(false);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create role: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        // Invalidate cache when a new role is created
        _cache.Remove(AllRolesCacheKey);

        return MapToRoleResponseDto(role, 0);
    }

    public async Task<bool> UpdateRoleAsync(UpdateRoleRequestDto model, CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByIdAsync(model.RoleId).ConfigureAwait(false);
        if (role == null)
        {
            return false;
        }

        role.Name = model.Name;
        var result = await _roleManager.UpdateAsync(role).ConfigureAwait(false);

        if (result.Succeeded)
        {
            // Invalidate cache when a role is updated
            _cache.Remove(AllRolesCacheKey);
        }

        return result.Succeeded;
    }

    public async Task<bool> DeleteRoleAsync(string roleId, CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByIdAsync(roleId).ConfigureAwait(false);
        if (role == null)
        {
            return false;
        }

        var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!).ConfigureAwait(false);
        if (usersInRole.Any())
        {
            throw new InvalidOperationException($"Cannot delete role. There are {usersInRole.Count} users assigned to this role.");
        }

        var result = await _roleManager.DeleteAsync(role).ConfigureAwait(false);

        if (result.Succeeded)
        {
            // Invalidate cache when a role is deleted
            _cache.Remove(AllRolesCacheKey);
        }

        return result.Succeeded;
    }

    public async Task<List<UserResponseDto>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken = default)
    {
        var users = await _userManager.GetUsersInRoleAsync(roleName).ConfigureAwait(false);
        var filteredUsers = users.Where(u => !u.IsDeleted).ToList();
        var result = new List<UserResponseDto>(filteredUsers.Count);

        // Batch load all roles for all users sequentially to avoid concurrent DbContext operations
        foreach (var user in filteredUsers)
        {
            var rolesForUser = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
            result.Add(MapToUserResponseDto(user, [.. rolesForUser]));
        }

        return result;
    }

    private static RoleResponseDto MapToRoleResponseDto(IdentityRole role, int userCount)
    {
        return new RoleResponseDto
        {
            Id = UniversalOpertaions.IfNullEmptyString(role.Id),
            Name = UniversalOpertaions.IfNullEmptyString(role.Name),
            UserCount = userCount
        };
    }

    private static UserResponseDto MapToUserResponseDto(AppUser user, List<string> roles)
    {
        return new UserResponseDto
        {
            Id = UniversalOpertaions.IfNullEmptyString(user.Id),
            Username = UniversalOpertaions.IfNullEmptyString(user.UserName),
            Email = UniversalOpertaions.IfNullEmptyString(user.Email),
            FullName = UniversalOpertaions.IfNullEmptyString(user.FullName),
            Gender = UniversalOpertaions.IfNullEmptyString(user.Gender),
            UserType = user.UserTypeId,
            IsActive = user.IsActive,
            IsVerified = user.IsVerified,
            CreatedOn = user.CreatedOn,
            Roles = roles
        };
    }
}
