// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BachatCommittee.Data.Entities;
using BachatCommittee.Data.Repos.Interfaces;
using BachatCommittee.Models.DTOs.Responses;
using BachatCommittee.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;

namespace BachatCommittee.Services;

/// <summary>
/// Service for managing permissions, roles, and user permissions.
/// </summary>
public class PermissionService(
    IPermissionRepository permissionRepository,
    IActionRepository actionRepository,
    IRolePermissionRepository rolePermissionRepository,
    IUserPermissionRepository userPermissionRepository,
    UserManager<Models.Classes.AppUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IMemoryCache cache) : IPermissionService
{
    private readonly IPermissionRepository _permissionRepository = permissionRepository;
    private readonly IActionRepository _actionRepository = actionRepository;
    private readonly IRolePermissionRepository _rolePermissionRepository = rolePermissionRepository;
    private readonly IUserPermissionRepository _userPermissionRepository = userPermissionRepository;
    private readonly UserManager<Models.Classes.AppUser> _userManager = userManager;
    private readonly RoleManager<IdentityRole> _roleManager = roleManager;
    private readonly IMemoryCache _cache = cache;

    private const string UserPermissionsCacheKeyPrefix = "UserPermissions:";
    private const string UserEffectivePermissionsCacheKeyPrefix = "UserEffectivePermissions:";
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(10);

    #region Permission Management

    public async Task<List<PermissionDto>> GetAllPermissionsAsync(CancellationToken cancellationToken = default)
    {
        var permissions = (await _permissionRepository.GetAllAsync(cancellationToken).ConfigureAwait(false)).ToList();
        return await MapToPermissionDtosAsync(permissions, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PermissionDto?> GetPermissionByIdAsync(Guid permissionId, CancellationToken cancellationToken = default)
    {
        var permission = await _permissionRepository.GetByIdAsync(permissionId, cancellationToken).ConfigureAwait(false);
        if (permission == null)
            return null;

        var permissions = new List<PermissionEntity> { permission };
        var dtos = await MapToPermissionDtosAsync(permissions, cancellationToken).ConfigureAwait(false);
        return dtos.FirstOrDefault();
    }

    public async Task<List<PermissionDto>> GetPermissionsByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        var permissions = (await _permissionRepository.GetByCategoryAsync(category, cancellationToken).ConfigureAwait(false)).ToList();
        return await MapToPermissionDtosAsync(permissions, cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region Role Permission Management

    public async Task<bool> AssignPermissionToRoleAsync(string roleId, Guid permissionId, Guid grantedBy, CancellationToken cancellationToken = default)
    {
        // Check if already exists
        var existing = await _rolePermissionRepository.GetByRoleAndPermissionAsync(roleId, permissionId, cancellationToken).ConfigureAwait(false);
        if (existing != null)
        {
            // If deleted, restore it
            if (existing.IsDeleted)
            {
                existing.IsDeleted = false;
                existing.IsActive = true;
                existing.CreatedBy = grantedBy;
                existing.CreatedOn = DateTime.UtcNow;
                existing.ModifiedOn = DateTime.UtcNow;
                existing.ModifiedBy = grantedBy;
                var result = await _rolePermissionRepository.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
                InvalidateRolePermissionCache(roleId);
                return result;
            }
            // If exists and active, return true
            if (existing.IsActive)
                return true;
            // If inactive, activate it
            existing.IsActive = true;
            existing.ModifiedOn = DateTime.UtcNow;
            existing.ModifiedBy = grantedBy;
            var result2 = await _rolePermissionRepository.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
            InvalidateRolePermissionCache(roleId);
            return result2;
        }

        // Create new
        var rolePermission = new RolePermissionEntity
        {
            RoleId = roleId,
            PermissionId = permissionId,
            CreatedBy = grantedBy,
            CreatedOn = DateTime.UtcNow,
            IsActive = true,
            IsDeleted = false
        };

        await _rolePermissionRepository.InsertAsync(rolePermission, cancellationToken).ConfigureAwait(false);
        InvalidateRolePermissionCache(roleId);
        return true;
    }

    public async Task<bool> RevokePermissionFromRoleAsync(string roleId, Guid permissionId, Guid revokedBy, CancellationToken cancellationToken = default)
    {
        var rolePermission = await _rolePermissionRepository.GetByRoleAndPermissionAsync(roleId, permissionId, cancellationToken).ConfigureAwait(false);
        if (rolePermission == null || rolePermission.IsDeleted)
            return false;

        rolePermission.IsActive = false;
        rolePermission.ModifiedOn = DateTime.UtcNow;
        rolePermission.ModifiedBy = revokedBy;

        var result = await _rolePermissionRepository.UpdateAsync(rolePermission, cancellationToken).ConfigureAwait(false);
        InvalidateRolePermissionCache(roleId);
        return result;
    }

    public async Task<List<PermissionDto>> GetRolePermissionsAsync(string roleId, CancellationToken cancellationToken = default)
    {
        // roleId can be either IdentityRole.Id or IdentityRole.Name
        // First try as ID, if not found, try as Name
        var role = await _roleManager.FindByIdAsync(roleId).ConfigureAwait(false);
        var actualRoleId = role?.Id ?? roleId;

        // If still not found by ID, try by name
        if (role == null)
        {
            role = await _roleManager.FindByNameAsync(roleId).ConfigureAwait(false);
            actualRoleId = role?.Id ?? roleId;
        }

        var rolePermissions = (await _rolePermissionRepository.GetByRoleIdAsync(actualRoleId, cancellationToken).ConfigureAwait(false)).ToList();
        var permissionIds = rolePermissions.Select(rp => rp.PermissionId).ToList();

        if (permissionIds.Count == 0)
            return [];

        var permissions = new List<PermissionEntity>();
        foreach (var permissionId in permissionIds)
        {
            var permission = await _permissionRepository.GetByIdAsync(permissionId, cancellationToken).ConfigureAwait(false);
            if (permission != null && !permission.IsDeleted)
                permissions.Add(permission);
        }

        return await MapToPermissionDtosAsync(permissions, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> AssignMultiplePermissionsToRoleAsync(string roleId, List<Guid> permissionIds, Guid grantedBy, CancellationToken cancellationToken = default)
    {
        foreach (var permissionId in permissionIds)
        {
            await AssignPermissionToRoleAsync(roleId, permissionId, grantedBy, cancellationToken).ConfigureAwait(false);
        }
        // Cache invalidation is handled in AssignPermissionToRoleAsync
        return true;
    }

    #endregion

    #region User Permission Management

    public async Task<bool> AssignPermissionToUserAsync(string userId, Guid permissionId, Guid grantedBy, DateTime? expiresOn = null, CancellationToken cancellationToken = default)
    {
        // Check if already exists
        var existing = await _userPermissionRepository.GetByUserAndPermissionAsync(userId, permissionId, cancellationToken).ConfigureAwait(false);
        if (existing != null)
        {
            // If deleted, restore it
            if (existing.IsDeleted)
            {
                existing.IsDeleted = false;
                existing.IsActive = true;
                existing.IsRevoked = false;
                existing.CreatedBy = grantedBy;
                existing.CreatedOn = DateTime.UtcNow;
                existing.ExpiresOn = expiresOn;
                existing.RevokedOn = null;
                existing.RevokedBy = null;
                existing.ModifiedOn = DateTime.UtcNow;
                existing.ModifiedBy = grantedBy;
                var result = await _userPermissionRepository.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
                InvalidateUserPermissionCache(userId);
                return result;
            }
            // If revoked, un-revoke it
            if (existing.IsRevoked)
            {
                existing.IsActive = true;
                existing.IsRevoked = false;
                existing.ExpiresOn = expiresOn;
                existing.RevokedOn = null;
                existing.RevokedBy = null;
                existing.ModifiedOn = DateTime.UtcNow;
                existing.ModifiedBy = grantedBy;
                var result = await _userPermissionRepository.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
                InvalidateUserPermissionCache(userId);
                return result;
            }
            // If exists and active, update expiry
            if (existing.IsActive)
            {
                existing.ExpiresOn = expiresOn;
                existing.ModifiedOn = DateTime.UtcNow;
                existing.ModifiedBy = grantedBy;
                var result = await _userPermissionRepository.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
                InvalidateUserPermissionCache(userId);
                return result;
            }
            // If inactive, activate it
            existing.IsActive = true;
            existing.IsRevoked = false;
            existing.ExpiresOn = expiresOn;
            existing.RevokedOn = null;
            existing.RevokedBy = null;
            existing.ModifiedOn = DateTime.UtcNow;
            existing.ModifiedBy = grantedBy;
            var result2 = await _userPermissionRepository.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
            InvalidateUserPermissionCache(userId);
            return result2;
        }

        // Create new
        var userPermission = new UserPermissionEntity
        {
            UserId = userId,
            PermissionId = permissionId,
            ExpiresOn = expiresOn,
            CreatedBy = grantedBy,
            CreatedOn = DateTime.UtcNow,
            IsActive = true,
            IsDeleted = false,
            IsRevoked = false
        };

        await _userPermissionRepository.InsertAsync(userPermission, cancellationToken).ConfigureAwait(false);
        InvalidateUserPermissionCache(userId);
        return true;
    }

    public async Task<bool> RevokePermissionFromUserAsync(string userId, Guid permissionId, Guid revokedBy, CancellationToken cancellationToken = default)
    {
        await _userPermissionRepository.RevokePermissionAsync(userId, permissionId, revokedBy, cancellationToken).ConfigureAwait(false);
        InvalidateUserPermissionCache(userId);
        return true;
    }

    public async Task<List<PermissionDto>> GetUserPermissionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var userPermissions = (await _userPermissionRepository.GetActivePermissionsByUserIdAsync(userId, cancellationToken).ConfigureAwait(false)).ToList();
        var permissionIds = userPermissions.Select(up => up.PermissionId).ToList();

        if (permissionIds.Count == 0)
        {
            return [];
        }

        var permissions = new List<PermissionEntity>();
        foreach (var permissionId in permissionIds)
        {
            var permission = await _permissionRepository.GetByIdAsync(permissionId, cancellationToken).ConfigureAwait(false);
            if (permission != null && !permission.IsDeleted)
            {
                permissions.Add(permission);
            }
        }

        return await MapToPermissionDtosAsync(permissions, cancellationToken).ConfigureAwait(false);
    }

    public async Task<UserEffectivePermissionsDto> GetUserEffectivePermissionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{UserEffectivePermissionsCacheKeyPrefix}{userId}";

        // Try to get from cache
        if (_cache.TryGetValue(cacheKey, out UserEffectivePermissionsDto? cachedPermissions) && cachedPermissions != null)
        {
            return cachedPermissions;
        }

        // Get user roles
        var user = await _userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (user == null)
            throw new InvalidOperationException($"User with ID {userId} not found.");

        var roles = (await _userManager.GetRolesAsync(user).ConfigureAwait(false)).ToList();

        // Get permissions from roles
        var rolePermissions = new List<PermissionEntity>();
        foreach (var roleName in roles)
        {
            var identityRole = await _roleManager.FindByNameAsync(roleName).ConfigureAwait(false);
            if (identityRole == null)
                continue;

            var rolePermissionEntities = (await _rolePermissionRepository.GetByRoleIdAsync(identityRole.Id, cancellationToken).ConfigureAwait(false)).ToList();
            foreach (var rp in rolePermissionEntities)
            {
                var permission = await _permissionRepository.GetByIdAsync(rp.PermissionId, cancellationToken).ConfigureAwait(false);
                if (permission != null && !permission.IsDeleted)
                    rolePermissions.Add(permission);
            }
        }

        // Get user-specific permissions
        var userPermissions = (await _userPermissionRepository.GetActivePermissionsByUserIdAsync(userId, cancellationToken).ConfigureAwait(false)).ToList();
        var userPermissionEntities = new List<PermissionEntity>();
        foreach (var up in userPermissions)
        {
            var permission = await _permissionRepository.GetByIdAsync(up.PermissionId, cancellationToken).ConfigureAwait(false);
            if (permission != null && !permission.IsDeleted)
                userPermissionEntities.Add(permission);
        }

        // Combine and deduplicate
        var allPermissions = rolePermissions
            .Concat(userPermissionEntities)
            .GroupBy(p => p.Id)
            .Select(g => g.First())
            .ToList();

        var result = new UserEffectivePermissionsDto
        {
            UserId = userId,
            Roles = roles,
            RolePermissions = await MapToPermissionDtosAsync(rolePermissions, cancellationToken).ConfigureAwait(false),
            UserPermissions = await MapToPermissionDtosAsync(userPermissionEntities, cancellationToken).ConfigureAwait(false),
            AllPermissions = await MapToPermissionDtosAsync(allPermissions, cancellationToken).ConfigureAwait(false)
        };

        // Cache the result
        _cache.Set(cacheKey, result, _cacheExpiration);

        return result;
    }

    #endregion

    #region Permission Checking

    public async Task<bool> HasPermissionAsync(string userId, string permissionName, CancellationToken cancellationToken = default)
    {
        var permission = await _permissionRepository.GetByNameAsync(permissionName, cancellationToken).ConfigureAwait(false);
        if (permission == null || permission.IsDeleted || !permission.IsActive)
            return false;

        return await HasPermissionAsync(userId, permission.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> HasPermissionAsync(string userId, string controllerName, string actionName, string httpMethod, CancellationToken cancellationToken = default)
    {
        var action = await _actionRepository.GetByControllerAndActionAsync(controllerName, actionName, httpMethod, cancellationToken).ConfigureAwait(false);
        if (action == null || action.IsDeleted || !action.IsActive)
            return false;

        var permission = await _permissionRepository.GetByActionIdAsync(action.Id, cancellationToken).ConfigureAwait(false);
        var permissionEntity = permission.FirstOrDefault(p => !p.IsDeleted && p.IsActive);
        if (permissionEntity == null)
            return false;

        return await HasPermissionAsync(userId, permissionEntity.Id, cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> HasPermissionAsync(string userId, Guid permissionId, CancellationToken cancellationToken)
    {
        // Get user's effective permission IDs from cache or database
        var userPermissionIds = await GetUserEffectivePermissionIdsAsync(userId, cancellationToken).ConfigureAwait(false);
        return userPermissionIds.Contains(permissionId);
    }

    /// <summary>
    /// Gets the set of permission IDs that the user has (from roles and user-specific permissions).
    /// Uses caching for performance.
    /// </summary>
    private async Task<HashSet<Guid>> GetUserEffectivePermissionIdsAsync(string userId, CancellationToken cancellationToken)
    {
        var cacheKey = $"{UserPermissionsCacheKeyPrefix}{userId}";

        // Try to get from cache
        if (_cache.TryGetValue(cacheKey, out HashSet<Guid>? cachedPermissionIds) && cachedPermissionIds != null)
        {
            return cachedPermissionIds;
        }

        // Get from database
        var permissionIds = new HashSet<Guid>();

        // Get user roles
        var user = await _userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (user != null)
        {
            var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);

            // Get permissions from roles
            foreach (var roleName in roles)
            {
                var identityRole = await _roleManager.FindByNameAsync(roleName).ConfigureAwait(false);
                if (identityRole == null)
                    continue;

                var rolePermissions = await _rolePermissionRepository.GetByRoleIdAsync(identityRole.Id, cancellationToken).ConfigureAwait(false);
                foreach (var rp in rolePermissions)
                {
                    if (!rp.IsDeleted && rp.IsActive)
                    {
                        permissionIds.Add(rp.PermissionId);
                    }
                }
            }
        }

        // Get user-specific permissions
        var userPermissions = await _userPermissionRepository.GetActivePermissionsByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);
        foreach (var up in userPermissions)
        {
            if (!up.IsDeleted && up.IsActive && !up.IsRevoked)
            {
                // Check expiration
                if (!up.ExpiresOn.HasValue || up.ExpiresOn.Value >= DateTime.UtcNow)
                {
                    permissionIds.Add(up.PermissionId);
                }
            }
        }

        // Cache the result
        _cache.Set(cacheKey, permissionIds, _cacheExpiration);

        return permissionIds;
    }

    #endregion

    #region Helper Methods

    private async Task<List<PermissionDto>> MapToPermissionDtosAsync(IEnumerable<PermissionEntity> permissions, CancellationToken cancellationToken)
    {
        var result = new List<PermissionDto>();

        foreach (var permission in permissions)
        {
            var action = await _actionRepository.GetByIdAsync(permission.ActionId, cancellationToken).ConfigureAwait(false);
            if (action == null)
                continue;

            result.Add(new PermissionDto
            {
                Id = permission.Id,
                Name = permission.Name,
                Category = permission.Category,
                Description = permission.Description,
                ActionId = permission.ActionId,
                ControllerName = action.ControllerName,
                ActionName = action.ActionName,
                HttpMethod = action.HttpMethod,
                Route = action.Route,
                IsActive = permission.IsActive
            });
        }

        return result;
    }

    /// <summary>
    /// Invalidates the permission cache for a specific user.
    /// </summary>
    private void InvalidateUserPermissionCache(string userId)
    {
        _cache.Remove($"{UserPermissionsCacheKeyPrefix}{userId}");
        _cache.Remove($"{UserEffectivePermissionsCacheKeyPrefix}{userId}");
    }

    /// <summary>
    /// Invalidates the permission cache for all users with a specific role.
    /// Note: This is a simple implementation that invalidates all user caches.
    /// For production, consider implementing a more targeted cache invalidation strategy.
    /// </summary>
    private static void InvalidateRolePermissionCache(string roleId)
    {
        // Invalidate all user permission caches since role permissions affect all users with that role
        // This is a simple approach - for better performance, consider tracking which users have which roles
        // For now, we'll let the cache expire naturally, or implement a more sophisticated invalidation strategy

        // Alternative: We could iterate through all users and invalidate only those with this role,
        // but that requires querying the database. For now, we'll rely on cache expiration.
        // In a production system with many users, consider using a distributed cache with tags/patterns.
    }

    #endregion
}

