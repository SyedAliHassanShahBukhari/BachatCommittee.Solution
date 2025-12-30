// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BachatCommittee.Models.DTOs.Responses;

namespace BachatCommittee.Services.Interfaces;

/// <summary>
/// Service interface for managing permissions, roles, and user permissions.
/// </summary>
public interface IPermissionService
{
    // Permission Management
    Task<List<PermissionDto>> GetAllPermissionsAsync(CancellationToken cancellationToken = default);
    Task<PermissionDto?> GetPermissionByIdAsync(Guid permissionId, CancellationToken cancellationToken = default);
    Task<List<PermissionDto>> GetPermissionsByCategoryAsync(string category, CancellationToken cancellationToken = default);

    // Role Permission Management
    Task<bool> AssignPermissionToRoleAsync(string roleId, Guid permissionId, Guid grantedBy, CancellationToken cancellationToken = default);
    Task<bool> RevokePermissionFromRoleAsync(string roleId, Guid permissionId, Guid revokedBy, CancellationToken cancellationToken = default);
    Task<List<PermissionDto>> GetRolePermissionsAsync(string roleId, CancellationToken cancellationToken = default);
    Task<bool> AssignMultiplePermissionsToRoleAsync(string roleId, List<Guid> permissionIds, Guid grantedBy, CancellationToken cancellationToken = default);

    // User Permission Management
    Task<bool> AssignPermissionToUserAsync(string userId, Guid permissionId, Guid grantedBy, DateTime? expiresOn = null, CancellationToken cancellationToken = default);
    Task<bool> RevokePermissionFromUserAsync(string userId, Guid permissionId, Guid revokedBy, CancellationToken cancellationToken = default);
    Task<List<PermissionDto>> GetUserPermissionsAsync(string userId, CancellationToken cancellationToken = default);
    Task<UserEffectivePermissionsDto> GetUserEffectivePermissionsAsync(string userId, CancellationToken cancellationToken = default);

    // Permission Checking
    Task<bool> HasPermissionAsync(string userId, string permissionName, CancellationToken cancellationToken = default);
    Task<bool> HasPermissionAsync(string userId, string controllerName, string actionName, string httpMethod, CancellationToken cancellationToken = default);
}

