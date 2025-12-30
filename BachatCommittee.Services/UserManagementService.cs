// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using awisk.common.Helpers;
using BachatCommittee.Models.Classes;
using BachatCommittee.Models.DTOs.Requests;
using BachatCommittee.Models.DTOs.Responses;
using BachatCommittee.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BachatCommittee.Services;

public class UserManagementService : IUserManagementService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IUserStore<AppUser> _userStore;
    private readonly IUserEmailStore<AppUser> _emailStore;

    public UserManagementService(
        UserManager<AppUser> userManager,
        IUserStore<AppUser> userStore)
    {
        _userManager = userManager;
        _userStore = userStore;
        _emailStore = (IUserEmailStore<AppUser>)_userStore;
    }

    public async Task<List<UserResponseDto>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userManager.Users
            .Where(u => !u.IsDeleted)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var result = new List<UserResponseDto>(users.Count);

        // Batch load all roles for all users in parallel to avoid N+1 query problem
        var roleTasks = users.Select(async user => new
        {
            User = user,
            Roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false)
        });

        var userRoles = await Task.WhenAll(roleTasks).ConfigureAwait(false);

        foreach (var userRole in userRoles)
        {
            result.Add(MapToUserResponseDto(userRole.User, userRole.Roles.ToList()));
        }

        return result;
    }

    public async Task<UserResponseDto?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (user == null || user.IsDeleted)
            return null;

        var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
        return MapToUserResponseDto(user, roles.ToList());
    }

    public async Task<UserResponseDto?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByNameAsync(username).ConfigureAwait(false);
        if (user == null || user.IsDeleted)
            return null;

        var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
        return MapToUserResponseDto(user, roles.ToList());
    }

    public async Task<UserResponseDto> CreateUserAsync(RegisterRequestDto model, Guid creatorUserId, CancellationToken cancellationToken = default)
    {
        var user = new AppUser();
        await _userStore.SetUserNameAsync(user, model.Username, cancellationToken).ConfigureAwait(false);
        await _emailStore.SetEmailAsync(user, model.Email, cancellationToken).ConfigureAwait(false);

        user.FullName = model.FullName;
        user.Gender = model.Gender;
        user.UserTypeId = model.UserType;
        user.IsActive = true;
        user.IsVerified = true;
        user.IsDeleted = false;
        user.CreatedOn = DateTime.UtcNow;
        user.CreatedBy = creatorUserId;

        var result = await _userManager.CreateAsync(user, model.Password).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        // Assign role based on UserType
        await _userManager.AddToRoleAsync(user, model.UserType.ToDescription()).ConfigureAwait(false);

        var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
        return MapToUserResponseDto(user, roles.ToList());
    }

    public async Task<bool> UpdateUserAsync(UpdateUserRequestDto model, Guid updaterUserId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(model.UserId).ConfigureAwait(false);
        if (user == null || user.IsDeleted)
            return false;

        await _userStore.SetUserNameAsync(user, model.Username, cancellationToken).ConfigureAwait(false);
        await _emailStore.SetEmailAsync(user, model.Email, cancellationToken).ConfigureAwait(false);

        user.FullName = model.FullName;
        user.Gender = model.Gender;
        user.UserTypeId = model.UserType;
        user.IsActive = model.IsActive;

        var result = await _userManager.UpdateAsync(user).ConfigureAwait(false);
        if (!result.Succeeded)
            return false;

        // Update roles
        var currentRoles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
        var rolesToRemove = currentRoles.Except(model.Roles).ToList();
        var rolesToAdd = model.Roles.Except(currentRoles).ToList();

        if (rolesToRemove.Any())
        {
            await _userManager.RemoveFromRolesAsync(user, rolesToRemove).ConfigureAwait(false);
        }

        if (rolesToAdd.Any())
        {
            await _userManager.AddToRolesAsync(user, rolesToAdd).ConfigureAwait(false);
        }

        return true;
    }

    public async Task<bool> DeleteUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (user == null || user.IsDeleted)
            return false;

        user.IsDeleted = true;
        user.IsActive = false;
        var result = await _userManager.UpdateAsync(user).ConfigureAwait(false);
        return result.Succeeded;
    }

    public async Task<bool> AssignRolesAsync(string userId, List<string> roles, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (user == null || user.IsDeleted)
            return false;

        var result = await _userManager.AddToRolesAsync(user, roles).ConfigureAwait(false);
        return result.Succeeded;
    }

    public async Task<bool> RemoveRolesAsync(string userId, List<string> roles, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (user == null || user.IsDeleted)
            return false;

        var result = await _userManager.RemoveFromRolesAsync(user, roles).ConfigureAwait(false);
        return result.Succeeded;
    }

    public async Task<List<string>> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (user == null || user.IsDeleted)
            return new List<string>();

        var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
        return roles.ToList();
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
