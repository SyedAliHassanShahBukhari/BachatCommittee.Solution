// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using awisk.common.DTOs.Responses;
using BachatCommittee.Models.DTOs.Requests;
using BachatCommittee.Models.DTOs.Responses;
using BachatCommittee.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BachatCommittee.API.Controllers;

/// <summary>
/// Controller for managing permissions, roles, and user permissions.
/// </summary>
[Route("api/v1/permissions")]
[ApiController]
[Authorize(Roles = "Developer,SuperAdmin")]
public class PermissionsController(
    IExceptionLogService exceptionLogService,
    IPermissionService permissionService,
    IActionDiscoveryService actionDiscoveryService,
    IUserContextService userContextService) : BaseAPIController(exceptionLogService)
{
    private readonly IPermissionService _permissionService = permissionService;
    private readonly IActionDiscoveryService _actionDiscoveryService = actionDiscoveryService;
    private readonly IUserContextService _userContextService = userContextService;

    #region Permissions Management

    /// <summary>
    /// Get all permissions.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(GenericResponseDto<List<PermissionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllPermissions(CancellationToken cancellationToken = default)
    {
        try
        {
            var permissions = await _permissionService.GetAllPermissionsAsync(cancellationToken).ConfigureAwait(false);
            return Ok(new GenericResponseDto<List<PermissionDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Message = "Permissions retrieved successfully.",
                Response = permissions
            });
        }
        catch (Exception ex)
        {
            return LogExceptionAndReturnResponse(ex, Request.Path);
        }
    }

    /// <summary>
    /// Get permission by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(GenericResponseDto<PermissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(GenericResponseDto<dynamic>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPermissionById(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var permission = await _permissionService.GetPermissionByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (permission == null)
            {
                return NotFound(new GenericResponseDto<dynamic>
                {
                    StatusCode = HttpStatusCode.NotFound,
                    Message = "Permission not found."
                });
            }

            return Ok(new GenericResponseDto<PermissionDto>
            {
                StatusCode = HttpStatusCode.OK,
                Message = "Permission retrieved successfully.",
                Response = permission
            });
        }
        catch (Exception ex)
        {
            return LogExceptionAndReturnResponse(ex, Request.Path);
        }
    }

    /// <summary>
    /// Get permissions by category.
    /// </summary>
    [HttpGet("category/{category}")]
    [ProducesResponseType(typeof(GenericResponseDto<List<PermissionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPermissionsByCategory(string category, CancellationToken cancellationToken = default)
    {
        try
        {
            var permissions = await _permissionService.GetPermissionsByCategoryAsync(category, cancellationToken).ConfigureAwait(false);
            return Ok(new GenericResponseDto<List<PermissionDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Message = "Permissions retrieved successfully.",
                Response = permissions
            });
        }
        catch (Exception ex)
        {
            return LogExceptionAndReturnResponse(ex, Request.Path);
        }
    }

    /// <summary>
    /// Manually trigger action discovery/sync.
    /// </summary>
    [HttpPost("sync")]
    [ProducesResponseType(typeof(GenericResponseDto<dynamic>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SyncActions(CancellationToken cancellationToken = default)
    {
        try
        {
            await _actionDiscoveryService.DiscoverAndRegisterActionsAsync(cancellationToken).ConfigureAwait(false);
            return Ok(new GenericResponseDto<dynamic>
            {
                StatusCode = HttpStatusCode.OK,
                Message = "Actions synchronized successfully."
            });
        }
        catch (Exception ex)
        {
            return LogExceptionAndReturnResponse(ex, Request.Path);
        }
    }

    #endregion

    #region Role Permissions Management

    /// <summary>
    /// Assign permission(s) to a role.
    /// </summary>
    [HttpPost("roles/{roleId}")]
    [ProducesResponseType(typeof(GenericResponseDto<dynamic>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(GenericResponseDto<dynamic>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AssignPermissionsToRole(
        string roleId,
        [FromBody] AssignPermissionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid || request == null || request.PermissionIds.Count == 0)
            {
                return BadRequest(new GenericResponseDto<dynamic>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Message = "Invalid request data."
                });
            }

            var grantedBy = _userContextService.LoggedInUser.Id;
            var success = await _permissionService.AssignMultiplePermissionsToRoleAsync(
                roleId,
                request.PermissionIds,
                grantedBy,
                cancellationToken).ConfigureAwait(false);

            if (!success)
            {
                return BadRequest(new GenericResponseDto<dynamic>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Message = "Failed to assign permissions to role."
                });
            }

            return Ok(new GenericResponseDto<dynamic>
            {
                StatusCode = HttpStatusCode.OK,
                Message = "Permissions assigned to role successfully."
            });
        }
        catch (Exception ex)
        {
            return LogExceptionAndReturnResponse(ex, Request.Path);
        }
    }

    /// <summary>
    /// Revoke a permission from a role.
    /// </summary>
    [HttpDelete("roles/{roleId}/{permissionId}")]
    [ProducesResponseType(typeof(GenericResponseDto<dynamic>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(GenericResponseDto<dynamic>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RevokePermissionFromRole(
        string roleId,
        Guid permissionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var revokedBy = _userContextService.LoggedInUser.Id;
            var success = await _permissionService.RevokePermissionFromRoleAsync(
                roleId,
                permissionId,
                revokedBy,
                cancellationToken).ConfigureAwait(false);

            if (!success)
            {
                return BadRequest(new GenericResponseDto<dynamic>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Message = "Failed to revoke permission from role."
                });
            }

            return Ok(new GenericResponseDto<dynamic>
            {
                StatusCode = HttpStatusCode.OK,
                Message = "Permission revoked from role successfully."
            });
        }
        catch (Exception ex)
        {
            return LogExceptionAndReturnResponse(ex, Request.Path);
        }
    }

    /// <summary>
    /// Get all permissions for a role.
    /// </summary>
    [HttpGet("roles/{roleId}")]
    [ProducesResponseType(typeof(GenericResponseDto<List<PermissionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRolePermissions(string roleId, CancellationToken cancellationToken = default)
    {
        try
        {
            var permissions = await _permissionService.GetRolePermissionsAsync(roleId, cancellationToken).ConfigureAwait(false);
            return Ok(new GenericResponseDto<List<PermissionDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Message = "Role permissions retrieved successfully.",
                Response = permissions
            });
        }
        catch (Exception ex)
        {
            return LogExceptionAndReturnResponse(ex, Request.Path);
        }
    }

    #endregion

    #region User Permissions Management

    /// <summary>
    /// Assign permission(s) to a user.
    /// </summary>
    [HttpPost("users/{userId}")]
    [ProducesResponseType(typeof(GenericResponseDto<dynamic>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(GenericResponseDto<dynamic>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AssignPermissionsToUser(
        string userId,
        [FromBody] AssignPermissionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid || request == null || request.PermissionIds.Count == 0)
            {
                return BadRequest(new GenericResponseDto<dynamic>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Message = "Invalid request data."
                });
            }

            var grantedBy = _userContextService.LoggedInUser.Id;

            // Assign each permission individually (since ExpiresOn is per permission)
            foreach (var permissionId in request.PermissionIds)
            {
                var success = await _permissionService.AssignPermissionToUserAsync(
                    userId,
                    permissionId,
                    grantedBy,
                    request.ExpiresOn,
                    cancellationToken).ConfigureAwait(false);

                if (!success)
                {
                    return BadRequest(new GenericResponseDto<dynamic>
                    {
                        StatusCode = HttpStatusCode.BadRequest,
                        Message = $"Failed to assign permission {permissionId} to user."
                    });
                }
            }

            return Ok(new GenericResponseDto<dynamic>
            {
                StatusCode = HttpStatusCode.OK,
                Message = "Permissions assigned to user successfully."
            });
        }
        catch (Exception ex)
        {
            return LogExceptionAndReturnResponse(ex, Request.Path);
        }
    }

    /// <summary>
    /// Revoke a permission from a user.
    /// </summary>
    [HttpDelete("users/{userId}/{permissionId}")]
    [ProducesResponseType(typeof(GenericResponseDto<dynamic>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(GenericResponseDto<dynamic>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RevokePermissionFromUser(
        string userId,
        Guid permissionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var revokedBy = _userContextService.LoggedInUser.Id;
            var success = await _permissionService.RevokePermissionFromUserAsync(
                userId,
                permissionId,
                revokedBy,
                cancellationToken).ConfigureAwait(false);

            if (!success)
            {
                return BadRequest(new GenericResponseDto<dynamic>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Message = "Failed to revoke permission from user."
                });
            }

            return Ok(new GenericResponseDto<dynamic>
            {
                StatusCode = HttpStatusCode.OK,
                Message = "Permission revoked from user successfully."
            });
        }
        catch (Exception ex)
        {
            return LogExceptionAndReturnResponse(ex, Request.Path);
        }
    }

    /// <summary>
    /// Get all permissions for a user (user-specific only, not from roles).
    /// </summary>
    [HttpGet("users/{userId}")]
    [ProducesResponseType(typeof(GenericResponseDto<List<PermissionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserPermissions(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var permissions = await _permissionService.GetUserPermissionsAsync(userId, cancellationToken).ConfigureAwait(false);
            return Ok(new GenericResponseDto<List<PermissionDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Message = "User permissions retrieved successfully.",
                Response = permissions
            });
        }
        catch (Exception ex)
        {
            return LogExceptionAndReturnResponse(ex, Request.Path);
        }
    }

    /// <summary>
    /// Get user's effective permissions (role permissions + user-specific permissions combined).
    /// </summary>
    [HttpGet("users/{userId}/effective")]
    [ProducesResponseType(typeof(GenericResponseDto<UserEffectivePermissionsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserEffectivePermissions(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var effectivePermissions = await _permissionService.GetUserEffectivePermissionsAsync(userId, cancellationToken).ConfigureAwait(false);
            return Ok(new GenericResponseDto<UserEffectivePermissionsDto>
            {
                StatusCode = HttpStatusCode.OK,
                Message = "User effective permissions retrieved successfully.",
                Response = effectivePermissions
            });
        }
        catch (Exception ex)
        {
            return LogExceptionAndReturnResponse(ex, Request.Path);
        }
    }

    #endregion
}

