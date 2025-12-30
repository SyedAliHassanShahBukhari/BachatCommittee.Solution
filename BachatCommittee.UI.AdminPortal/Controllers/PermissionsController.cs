// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using awisk.common.DTOs.Responses;
using awisk.common.Interfaces;
using BachatCommittee.Models.DTOs.Requests;
using BachatCommittee.Models.DTOs.Responses;
using BachatCommittee.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace BachatCommittee.UI.AdminPortal.Controllers;

[Authorize(Roles = "Developer,SuperAdmin")]
public class PermissionsController(IApiService apiService, ILogger<PermissionsController> logger, IUserContextService userContextService) : Controller
{
    private readonly IApiService _apiService = apiService;
    private readonly ILogger<PermissionsController> _logger = logger;
    private readonly IUserContextService _userContextService = userContextService;

    public async Task<IActionResult> Index()
    {
        try
        {
            var response = await _apiService.GetAsync<GenericResponseDto<List<PermissionDto>>>(new Uri("permissions", UriKind.Relative), _userContextService.Token).ConfigureAwait(true);
            ViewBag.Permissions = response?.Response ?? [];
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading permissions");
            ViewBag.Error = "Error loading permissions. Please try again.";
            ViewBag.Permissions = new List<PermissionDto>();
            return View();
        }
    }

    [HttpGet]
    public async Task<IActionResult> ByCategory(string category)
    {
        try
        {
            var response = await _apiService.GetAsync<GenericResponseDto<List<PermissionDto>>>(new Uri($"permissions/category/{category}", UriKind.Relative), _userContextService.Token).ConfigureAwait(true);
            ViewBag.Permissions = response?.Response ?? [];
            ViewBag.Category = category;
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading permissions by category");
            ViewBag.Error = "Error loading permissions. Please try again.";
            ViewBag.Permissions = new List<PermissionDto>();
            ViewBag.Category = category;
            return View();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncActions()
    {
        try
        {
            var response = await _apiService.PostAsync<object, GenericResponseDto<object>>(new Uri("permissions/sync", UriKind.Relative), new { }, _userContextService.Token).ConfigureAwait(true);

            if (response?.StatusCode == System.Net.HttpStatusCode.OK)
            {
                TempData["SuccessMessage"] = "Actions synchronized successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = response?.Message ?? "Failed to sync actions.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing actions");
            TempData["ErrorMessage"] = "An error occurred while syncing actions.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> RolePermissions(string roleId)
    {
        try
        {
            // Get role details
            var roleResponse = await _apiService.GetAsync<GenericResponseDto<RoleResponseDto>>(new Uri($"roles/{roleId}", UriKind.Relative), _userContextService.Token).ConfigureAwait(true);
            ViewBag.Role = roleResponse?.Response;

            // Get all permissions
            var allPermissionsResponse = await _apiService.GetAsync<GenericResponseDto<List<PermissionDto>>>(new Uri("permissions", UriKind.Relative), _userContextService.Token).ConfigureAwait(true);
            ViewBag.AllPermissions = allPermissionsResponse?.Response ?? [];

            // Get role permissions
            var rolePermissionsResponse = await _apiService.GetAsync<GenericResponseDto<List<PermissionDto>>>(new Uri($"permissions/roles/{roleId}", UriKind.Relative), _userContextService.Token).ConfigureAwait(true);
            ViewBag.RolePermissions = rolePermissionsResponse?.Response ?? [];

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading role permissions");
            TempData["ErrorMessage"] = "Error loading role permissions.";
            return RedirectToAction("Index", "Roles");
        }
    }

    [HttpPost]
    public async Task<IActionResult> AssignPermissionsToRole(string roleId, [FromBody] AssignPermissionRequestDto request)
    {
        try
        {
            var response = await _apiService.PostAsync<AssignPermissionRequestDto, GenericResponseDto<object>>(
                new Uri($"permissions/roles/{roleId}", UriKind.Relative),
                request,
                _userContextService.Token).ConfigureAwait(true);

            if (response?.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return Json(new { success = true, message = "Permissions assigned successfully." });
            }

            return Json(new { success = false, message = response?.Message ?? "Failed to assign permissions." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning permissions to role");
            return Json(new { success = false, message = "An error occurred while assigning permissions." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokePermissionFromRole(string roleId, Guid permissionId)
    {
        try
        {
            var response = await _apiService.DeleteAsync(new Uri($"permissions/roles/{roleId}/{permissionId}", UriKind.Relative), _userContextService.Token).ConfigureAwait(true);

            if (response)
            {
                TempData["SuccessMessage"] = "Permission revoked successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to revoke permission.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking permission from role");
            TempData["ErrorMessage"] = "An error occurred while revoking permission.";
        }

        return RedirectToAction(nameof(RolePermissions), new { roleId });
    }

    [HttpGet]
    public async Task<IActionResult> UserPermissions(string userId)
    {
        try
        {
            // Get user details
            var userResponse = await _apiService.GetAsync<GenericResponseDto<UserResponseDto>>(new Uri($"users/{userId}", UriKind.Relative), _userContextService.Token).ConfigureAwait(true);
            ViewBag.User = userResponse?.Response;

            // Get all permissions
            var allPermissionsResponse = await _apiService.GetAsync<GenericResponseDto<List<PermissionDto>>>(new Uri("permissions", UriKind.Relative), _userContextService.Token).ConfigureAwait(true);
            ViewBag.AllPermissions = allPermissionsResponse?.Response ?? [];

            // Get user permissions
            var userPermissionsResponse = await _apiService.GetAsync<GenericResponseDto<List<PermissionDto>>>(new Uri($"permissions/users/{userId}", UriKind.Relative), _userContextService.Token).ConfigureAwait(true);
            ViewBag.UserPermissions = userPermissionsResponse?.Response ?? [];

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user permissions");
            TempData["ErrorMessage"] = "Error loading user permissions.";
            return RedirectToAction("Index", "Users");
        }
    }

    [HttpPost]
    public async Task<IActionResult> AssignPermissionsToUser(string userId, [FromBody] AssignPermissionRequestDto request)
    {
        try
        {
            var response = await _apiService.PostAsync<AssignPermissionRequestDto, GenericResponseDto<object>>(
                new Uri($"permissions/users/{userId}", UriKind.Relative),
                request,
                _userContextService.Token).ConfigureAwait(true);

            if (response?.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return Json(new { success = true, message = "Permissions assigned successfully." });
            }

            return Json(new { success = false, message = response?.Message ?? "Failed to assign permissions." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning permissions to user");
            return Json(new { success = false, message = "An error occurred while assigning permissions." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokePermissionFromUser(string userId, Guid permissionId)
    {
        try
        {
            var response = await _apiService.DeleteAsync(new Uri($"permissions/users/{userId}/{permissionId}", UriKind.Relative), _userContextService.Token).ConfigureAwait(true);

            if (response)
            {
                TempData["SuccessMessage"] = "Permission revoked successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to revoke permission.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking permission from user");
            TempData["ErrorMessage"] = "An error occurred while revoking permission.";
        }

        return RedirectToAction(nameof(UserPermissions), new { userId });
    }

    [HttpGet]
    public async Task<IActionResult> UserEffectivePermissions(string userId)
    {
        try
        {
            // Get user details
            var userResponse = await _apiService.GetAsync<GenericResponseDto<UserResponseDto>>(new Uri($"users/{userId}", UriKind.Relative), _userContextService.Token).ConfigureAwait(true);
            ViewBag.User = userResponse?.Response;

            // Get effective permissions
            var response = await _apiService.GetAsync<GenericResponseDto<UserEffectivePermissionsDto>>(new Uri($"permissions/users/{userId}/effective", UriKind.Relative), _userContextService.Token).ConfigureAwait(true);
            ViewBag.EffectivePermissions = response?.Response;

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user effective permissions");
            TempData["ErrorMessage"] = "Error loading user effective permissions.";
            return RedirectToAction("Index", "Users");
        }
    }
}

