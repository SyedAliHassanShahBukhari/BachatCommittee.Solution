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
public class RolesController(IApiService apiService, ILogger<RolesController> logger, IUserContextService userContextService) : Controller
{
    private readonly IApiService _apiService = apiService;
    private readonly ILogger<RolesController> _logger = logger;
    private readonly IUserContextService _userContextService = userContextService;

    public async Task<IActionResult> Index()
    {
        try
        {
            var response = await _apiService.GetAsync<GenericResponseDto<List<RoleResponseDto>>>(new Uri("roles", UriKind.Relative), _userContextService.Token).ConfigureAwait(true);
            ViewBag.Roles = response?.Response ?? [];
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading roles");
            ViewBag.Error = "Error loading roles. Please try again.";
            ViewBag.Roles = new List<RoleResponseDto>();
            return View();
        }
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateRoleRequestDto model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var response = await _apiService.PostAsync<CreateRoleRequestDto, GenericResponseDto<RoleResponseDto>>(new Uri("roles", UriKind.Relative), model, _userContextService.Token).ConfigureAwait(true);

            if (response?.StatusCode == System.Net.HttpStatusCode.Created)
            {
                TempData["SuccessMessage"] = "Role created successfully.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, response?.Message ?? "Failed to create role.");
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role");
            ModelState.AddModelError(string.Empty, "An error occurred while creating the role.");
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        try
        {
            var response = await _apiService.GetAsync<GenericResponseDto<RoleResponseDto>>(new Uri($"roles/{id}", UriKind.Relative), _userContextService.Token).ConfigureAwait(true);
            if (response?.Response == null)
            {
                TempData["ErrorMessage"] = "Role not found.";
                return RedirectToAction(nameof(Index));
            }

            var model = new UpdateRoleRequestDto
            {
                RoleId = response.Response.Id,
                Name = response.Response.Name
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading role for edit");
            TempData["ErrorMessage"] = "Error loading role.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateRoleRequestDto model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var response = await _apiService.PutAsync<UpdateRoleRequestDto, GenericResponseDto<object>>(new Uri($"roles/{model.RoleId}", UriKind.Relative), model, _userContextService.Token).ConfigureAwait(true);

            if (response?.StatusCode == System.Net.HttpStatusCode.OK)
            {
                TempData["SuccessMessage"] = "Role updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, response?.Message ?? "Failed to update role.");
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role");
            ModelState.AddModelError(string.Empty, "An error occurred while updating the role.");
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            var response = await _apiService.DeleteAsync(new Uri($"roles/{id}", UriKind.Relative), _userContextService.Token).ConfigureAwait(true);

            if (response)
            {
                TempData["SuccessMessage"] = "Role deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete role.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting role");
            TempData["ErrorMessage"] = "An error occurred while deleting the role.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Users(string roleName)
    {
        try
        {
            var response = await _apiService.GetAsync<GenericResponseDto<List<UserResponseDto>>>(new Uri($"roles/{roleName}/users", UriKind.Relative), _userContextService.Token).ConfigureAwait(true);
            ViewBag.Users = response?.Response ?? [];
            ViewBag.RoleName = roleName;
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading users in role");
            ViewBag.Error = "Error loading users in role.";
            ViewBag.Users = new List<UserResponseDto>();
            ViewBag.RoleName = roleName;
            return View();
        }
    }
}
