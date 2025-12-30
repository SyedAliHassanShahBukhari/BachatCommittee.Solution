// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using awisk.common.DTOs.Responses;
using awisk.common.Interfaces;
using BachatCommittee.Models.DTOs.Requests;
using BachatCommittee.Models.DTOs.Responses;
using BachatCommittee.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BachatCommittee.UI.AdminPortal.Controllers;

[Authorize(Roles = "Developer,SuperAdmin")]
public class UsersController(IApiService apiService, ILogger<UsersController> logger, IUserContextService userContextService) : Controller
{
    private readonly IApiService _apiService = apiService;
    private readonly ILogger<UsersController> _logger = logger;
    private readonly IUserContextService _userContextService = userContextService;

    public async Task<IActionResult> Index()
    {
        try
        {
            var response = await _apiService.GetAsync<GenericResponseDto<List<UserResponseDto>>>(new Uri("users", UriKind.Relative), _userContextService.Token).ConfigureAwait(true);
            ViewBag.Users = response?.Response ?? [];
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading users");
            ViewBag.Error = "Error loading users. Please try again.";
            ViewBag.Users = new List<UserResponseDto>();
            return View();
        }
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        try
        {
            var rolesResponse = await _apiService.GetAsync<GenericResponseDto<List<RoleResponseDto>>>(new Uri("roles", UriKind.Relative), _userContextService.Token).ConfigureAwait(true);
            ViewBag.Roles = rolesResponse?.Response ?? [];
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading roles");
            ViewBag.Roles = new List<RoleResponseDto>();
            return View();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RegisterRequestDto model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var rolesResponse = await _apiService.GetAsync<GenericResponseDto<List<RoleResponseDto>>>(new Uri("roles", UriKind.Relative), _userContextService.Token).ConfigureAwait(true);
                ViewBag.Roles = rolesResponse?.Response ?? [];
                return View(model);
            }

            var response = await _apiService.PostAsync<RegisterRequestDto, GenericResponseDto<UserResponseDto>>(new Uri("users", UriKind.Relative), model, _userContextService.Token).ConfigureAwait(true);

            if (response?.StatusCode == System.Net.HttpStatusCode.Created)
            {
                TempData["SuccessMessage"] = "User created successfully.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, response?.Message ?? "Failed to create user.");
            var rolesResponse2 = await _apiService.GetAsync<GenericResponseDto<List<RoleResponseDto>>>(new Uri("roles", UriKind.Relative), _userContextService.Token).ConfigureAwait(true);
            ViewBag.Roles = rolesResponse2?.Response ?? [];
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            ModelState.AddModelError(string.Empty, "An error occurred while creating the user.");
            var rolesResponse = await _apiService.GetAsync<GenericResponseDto<List<RoleResponseDto>>>(new Uri("roles", UriKind.Relative), _userContextService.Token).ConfigureAwait(true);
            ViewBag.Roles = rolesResponse?.Response ?? [];
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        try
        {
            var userResponse = await _apiService.GetAsync<GenericResponseDto<UserResponseDto>>(new Uri($"users/{id}", UriKind.Relative), _userContextService.Token).ConfigureAwait(true);
            if (userResponse?.Response == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            var rolesResponse = await _apiService.GetAsync<GenericResponseDto<List<RoleResponseDto>>>(new Uri("roles", UriKind.Relative), _userContextService.Token).ConfigureAwait(true);
            ViewBag.Roles = rolesResponse?.Response ?? [];
            ViewBag.UserRoles = userResponse.Response.Roles;

            var model = new UpdateUserRequestDto
            {
                UserId = userResponse.Response.Id,
                Username = userResponse.Response.Username,
                Email = userResponse.Response.Email,
                FullName = userResponse.Response.FullName,
                Gender = userResponse.Response.Gender,
                UserType = userResponse.Response.UserType,
                IsActive = userResponse.Response.IsActive,
                Roles = userResponse.Response.Roles
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user for edit");
            TempData["ErrorMessage"] = "Error loading user.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateUserRequestDto model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var rolesResponse = await _apiService.GetAsync<GenericResponseDto<List<RoleResponseDto>>>(new Uri("roles", UriKind.Relative), _userContextService.Token).ConfigureAwait(true);
                ViewBag.Roles = rolesResponse?.Response ?? [];
                ViewBag.UserRoles = model.Roles;
                return View(model);
            }

            var response = await _apiService.PutAsync<UpdateUserRequestDto, GenericResponseDto<object>>(new Uri($"users/{model.UserId}", UriKind.Relative), model, _userContextService.Token).ConfigureAwait(true);

            if (response?.StatusCode == System.Net.HttpStatusCode.OK)
            {
                TempData["SuccessMessage"] = "User updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, response?.Message ?? "Failed to update user.");
            var rolesResponse2 = await _apiService.GetAsync<GenericResponseDto<List<RoleResponseDto>>>(new Uri("roles", UriKind.Relative), _userContextService.Token).ConfigureAwait(true);
            ViewBag.Roles = rolesResponse2?.Response ?? [];
            ViewBag.UserRoles = model.Roles;
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user");
            ModelState.AddModelError(string.Empty, "An error occurred while updating the user.");
            var rolesResponse = await _apiService.GetAsync<GenericResponseDto<List<RoleResponseDto>>>(new Uri("roles", UriKind.Relative), _userContextService.Token).ConfigureAwait(true);
            ViewBag.Roles = rolesResponse?.Response ?? [];
            ViewBag.UserRoles = model.Roles;
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            var response = await _apiService.DeleteAsync(new Uri($"users/{id}", UriKind.Relative), _userContextService.Token).ConfigureAwait(true);

            if (response)
            {
                TempData["SuccessMessage"] = "User deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete user.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user");
            TempData["ErrorMessage"] = "An error occurred while deleting the user.";
        }

        return RedirectToAction(nameof(Index));
    }
}
