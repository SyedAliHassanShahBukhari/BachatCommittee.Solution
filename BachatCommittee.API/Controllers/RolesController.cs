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

[Route("api/v1/[controller]")]
[ApiController]
[Authorize(Roles = "Developer,SuperAdmin")]
public class RolesController(IExceptionLogService exceptionLogService, IRoleManagementService roleManagementService) : BaseAPIController(exceptionLogService)
{
    private readonly IRoleManagementService _roleManagementService = roleManagementService;

    [HttpGet]
    [ProducesResponseType(typeof(GenericResponseDto<List<RoleResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllRoles(CancellationToken cancellationToken = default)
    {
        try
        {
            var roles = await _roleManagementService.GetAllRolesAsync(cancellationToken).ConfigureAwait(false);
            return Ok(new GenericResponseDto<List<RoleResponseDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Message = "Roles retrieved successfully.",
                Response = roles
            });
        }
        catch (Exception ex)
        {
            return LogExceptionAndReturnResponse(ex, Request.Path);
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(GenericResponseDto<RoleResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(GenericResponseDto<dynamic>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoleById(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var role = await _roleManagementService.GetRoleByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (role == null)
            {
                return NotFound(new GenericResponseDto<dynamic>
                {
                    StatusCode = HttpStatusCode.NotFound,
                    Message = "Role not found."
                });
            }

            return Ok(new GenericResponseDto<RoleResponseDto>
            {
                StatusCode = HttpStatusCode.OK,
                Message = "Role retrieved successfully.",
                Response = role
            });
        }
        catch (Exception ex)
        {
            return LogExceptionAndReturnResponse(ex, Request.Path);
        }
    }

    [HttpGet("name/{name}")]
    [ProducesResponseType(typeof(GenericResponseDto<RoleResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(GenericResponseDto<dynamic>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoleByName(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            var role = await _roleManagementService.GetRoleByNameAsync(name, cancellationToken).ConfigureAwait(false);
            if (role == null)
            {
                return NotFound(new GenericResponseDto<dynamic>
                {
                    StatusCode = HttpStatusCode.NotFound,
                    Message = "Role not found."
                });
            }

            return Ok(new GenericResponseDto<RoleResponseDto>
            {
                StatusCode = HttpStatusCode.OK,
                Message = "Role retrieved successfully.",
                Response = role
            });
        }
        catch (Exception ex)
        {
            return LogExceptionAndReturnResponse(ex, Request.Path);
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(GenericResponseDto<RoleResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(GenericResponseDto<dynamic>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequestDto model, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid || model == null)
            {
                return BadRequest(new GenericResponseDto<dynamic>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Message = "Invalid role data."
                });
            }

            var role = await _roleManagementService.CreateRoleAsync(model, cancellationToken).ConfigureAwait(false);

            return CreatedAtAction(nameof(GetRoleById), new { id = role.Id }, new GenericResponseDto<RoleResponseDto>
            {
                StatusCode = HttpStatusCode.Created,
                Message = "Role created successfully.",
                Response = role
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new GenericResponseDto<dynamic>
            {
                StatusCode = HttpStatusCode.BadRequest,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            return LogExceptionAndReturnResponse(ex, Request.Path);
        }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(GenericResponseDto<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(GenericResponseDto<dynamic>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRole(string id, [FromBody] UpdateRoleRequestDto model, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid || model == null)
            {
                return BadRequest(new GenericResponseDto<dynamic>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Message = "Invalid role data."
                });
            }

            if (model.RoleId != id)
            {
                return BadRequest(new GenericResponseDto<dynamic>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Message = "Role ID mismatch."
                });
            }

            var result = await _roleManagementService.UpdateRoleAsync(model, cancellationToken).ConfigureAwait(false);

            if (!result)
            {
                return NotFound(new GenericResponseDto<dynamic>
                {
                    StatusCode = HttpStatusCode.NotFound,
                    Message = "Role not found or update failed."
                });
            }

            return Ok(new GenericResponseDto<object>
            {
                StatusCode = HttpStatusCode.OK,
                Message = "Role updated successfully."
            });
        }
        catch (Exception ex)
        {
            return LogExceptionAndReturnResponse(ex, Request.Path);
        }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(GenericResponseDto<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(GenericResponseDto<dynamic>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(GenericResponseDto<dynamic>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRole(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _roleManagementService.DeleteRoleAsync(id, cancellationToken).ConfigureAwait(false);

            if (!result)
            {
                return NotFound(new GenericResponseDto<dynamic>
                {
                    StatusCode = HttpStatusCode.NotFound,
                    Message = "Role not found or delete failed."
                });
            }

            return Ok(new GenericResponseDto<object>
            {
                StatusCode = HttpStatusCode.OK,
                Message = "Role deleted successfully."
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new GenericResponseDto<dynamic>
            {
                StatusCode = HttpStatusCode.BadRequest,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            return LogExceptionAndReturnResponse(ex, Request.Path);
        }
    }

    [HttpGet("{roleName}/users")]
    [ProducesResponseType(typeof(GenericResponseDto<List<UserResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsersInRole(string roleName, CancellationToken cancellationToken = default)
    {
        try
        {
            var users = await _roleManagementService.GetUsersInRoleAsync(roleName, cancellationToken).ConfigureAwait(false);
            return Ok(new GenericResponseDto<List<UserResponseDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Message = "Users in role retrieved successfully.",
                Response = users
            });
        }
        catch (Exception ex)
        {
            return LogExceptionAndReturnResponse(ex, Request.Path);
        }
    }
}
