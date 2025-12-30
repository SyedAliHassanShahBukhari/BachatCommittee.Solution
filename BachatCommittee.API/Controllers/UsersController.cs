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
public class UsersController(IExceptionLogService exceptionLogService, IUserManagementService userManagementService, IUserContextService userContextService) : BaseAPIController(exceptionLogService)
{
    private readonly IUserManagementService _userManagementService = userManagementService;
    private readonly IUserContextService _userContextService = userContextService;

    [HttpGet]
    [ProducesResponseType(typeof(GenericResponseDto<List<UserResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllUsers(CancellationToken cancellationToken = default)
    {
        try
        {
            var users = await _userManagementService.GetAllUsersAsync(cancellationToken).ConfigureAwait(false);
            return Ok(new GenericResponseDto<List<UserResponseDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Message = "Users retrieved successfully.",
                Response = users
            });
        }
        catch (Exception ex)
        {
            return LogExceptionAndReturnResponse(ex, Request.Path);
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(GenericResponseDto<UserResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(GenericResponseDto<dynamic>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userManagementService.GetUserByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (user == null)
            {
                return NotFound(new GenericResponseDto<dynamic>
                {
                    StatusCode = HttpStatusCode.NotFound,
                    Message = "User not found."
                });
            }

            return Ok(new GenericResponseDto<UserResponseDto>
            {
                StatusCode = HttpStatusCode.OK,
                Message = "User retrieved successfully.",
                Response = user
            });
        }
        catch (Exception ex)
        {
            return LogExceptionAndReturnResponse(ex, Request.Path);
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(GenericResponseDto<UserResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(GenericResponseDto<dynamic>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateUser([FromBody] RegisterRequestDto model, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid || model == null)
            {
                return BadRequest(new GenericResponseDto<dynamic>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Message = "Invalid user data."
                });
            }

            var creatorId = _userContextService.LoggedInUser.Id;
            var user = await _userManagementService.CreateUserAsync(model, creatorId, cancellationToken).ConfigureAwait(false);

            return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, new GenericResponseDto<UserResponseDto>
            {
                StatusCode = HttpStatusCode.Created,
                Message = "User created successfully.",
                Response = user
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
    public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserRequestDto model, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid || model == null)
            {
                return BadRequest(new GenericResponseDto<dynamic>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Message = "Invalid user data."
                });
            }

            if (model.UserId != id)
            {
                return BadRequest(new GenericResponseDto<dynamic>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Message = "User ID mismatch."
                });
            }

            var updaterId = _userContextService.LoggedInUser.Id;
            var result = await _userManagementService.UpdateUserAsync(model, updaterId, cancellationToken).ConfigureAwait(false);

            if (!result)
            {
                return NotFound(new GenericResponseDto<dynamic>
                {
                    StatusCode = HttpStatusCode.NotFound,
                    Message = "User not found or update failed."
                });
            }

            return Ok(new GenericResponseDto<object>
            {
                StatusCode = HttpStatusCode.OK,
                Message = "User updated successfully."
            });
        }
        catch (Exception ex)
        {
            return LogExceptionAndReturnResponse(ex, Request.Path);
        }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(GenericResponseDto<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(GenericResponseDto<dynamic>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _userManagementService.DeleteUserAsync(id, cancellationToken).ConfigureAwait(false);

            if (!result)
            {
                return NotFound(new GenericResponseDto<dynamic>
                {
                    StatusCode = HttpStatusCode.NotFound,
                    Message = "User not found or delete failed."
                });
            }

            return Ok(new GenericResponseDto<object>
            {
                StatusCode = HttpStatusCode.OK,
                Message = "User deleted successfully."
            });
        }
        catch (Exception ex)
        {
            return LogExceptionAndReturnResponse(ex, Request.Path);
        }
    }

    [HttpPost("{id}/roles/assign")]
    [ProducesResponseType(typeof(GenericResponseDto<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(GenericResponseDto<dynamic>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignRoles(string id, [FromBody] AssignRolesRequestDto model, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid || model == null || model.Roles == null || model.Roles.Count == 0)
            {
                return BadRequest(new GenericResponseDto<dynamic>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Message = "Invalid role assignment data."
                });
            }

            if (model.UserId != id)
            {
                return BadRequest(new GenericResponseDto<dynamic>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Message = "User ID mismatch."
                });
            }

            var result = await _userManagementService.AssignRolesAsync(id, model.Roles, cancellationToken).ConfigureAwait(false);

            if (!result)
            {
                return NotFound(new GenericResponseDto<dynamic>
                {
                    StatusCode = HttpStatusCode.NotFound,
                    Message = "User not found or role assignment failed."
                });
            }

            return Ok(new GenericResponseDto<object>
            {
                StatusCode = HttpStatusCode.OK,
                Message = "Roles assigned successfully."
            });
        }
        catch (Exception ex)
        {
            return LogExceptionAndReturnResponse(ex, Request.Path);
        }
    }

    [HttpGet("{id}/roles")]
    [ProducesResponseType(typeof(GenericResponseDto<List<string>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserRoles(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var roles = await _userManagementService.GetUserRolesAsync(id, cancellationToken).ConfigureAwait(false);
            return Ok(new GenericResponseDto<List<string>>
            {
                StatusCode = HttpStatusCode.OK,
                Message = "User roles retrieved successfully.",
                Response = roles
            });
        }
        catch (Exception ex)
        {
            return LogExceptionAndReturnResponse(ex, Request.Path);
        }
    }
}
