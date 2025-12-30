// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using awisk.common.DTOs.Responses;
using BachatCommittee.Models.DTOs.Requests;
using BachatCommittee.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BachatCommittee.API.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class AuthController(IAuthService auth, IUserContextService userContextService) : ControllerBase
{
    private readonly IAuthService _auth = auth;
    private readonly IUserContextService _userContextService = userContextService;

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GenericResponseDto<TokenResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(GenericResponseDto<dynamic>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(GenericResponseDto<dynamic>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(GenericResponseDto<dynamic>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto model)
    {
        if (!ModelState.IsValid || model is null)
        {
            return BadRequest(new GenericResponseDto<dynamic>
            {
                StatusCode = HttpStatusCode.BadRequest,
                Message = "Invalid payload."
            });
        }

        var tokenPayload = await _auth.TryLoginAsync(model).ConfigureAwait(false);
        if (tokenPayload is null)
        {
            return Unauthorized(new GenericResponseDto<dynamic>
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Message = "Invalid login."
            });
        }

        return CreatedAtAction(nameof(Login), new GenericResponseDto<TokenResponseDto>
        {
            StatusCode = HttpStatusCode.Created,
            Message = "Login successful.",
            Response = tokenPayload
        });
    }

    [HttpPut("update-user-password")]
    [Authorize(Roles = "Developer,SuperAdmin")]
    [ProducesResponseType(typeof(GenericResponseDto<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(GenericResponseDto<dynamic>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(GenericResponseDto<dynamic>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ChangeUserPassword([FromBody] ChangeUserPasswordRequestDto request)
    {
        if (string.IsNullOrEmpty(request.UserId) || string.IsNullOrEmpty(request.Password))
        {
            return BadRequest(new GenericResponseDto<dynamic>
            {
                StatusCode = HttpStatusCode.BadRequest,
                Message = "Invalid change password data."
            });
        }
        if (request.Password.Length < 6 || request.Password.Length > 100)
        {
            return BadRequest(new GenericResponseDto<dynamic>
            {
                StatusCode = HttpStatusCode.BadRequest,
                Message = "Password length must be within the range of 6 to 100 characters."
            });
        }
        if (!_auth.IsValidPassword(request.Password))
        {
            return BadRequest(new GenericResponseDto<dynamic>
            {
                StatusCode = HttpStatusCode.BadRequest,
                Message = "Password is not valid, password must have one samll, one capital, one number and on special character. The allowed special charaters are !@^*()-_=+[]{};:,."
            });
        }
        var response = await _auth.ChangeUserPassword(request.UserId, request.Password).ConfigureAwait(false);
        if (response)
        {
            return Ok(new GenericResponseDto<dynamic>
            {
                StatusCode = HttpStatusCode.OK,
                Message = "Password updated successfully"
            });
        }
        else
        {

            return BadRequest(new GenericResponseDto<dynamic>
            {
                StatusCode = HttpStatusCode.BadRequest,
                Message = "Password updation failed. Please try again later or contact administrator."
            });
        }
    }

    [HttpPost("register-user")]
    [Authorize(Roles = "Developer,SuperAdmin")]
    [ProducesResponseType(typeof(GenericResponseDto<TokenResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(GenericResponseDto<dynamic>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(GenericResponseDto<dynamic>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RegisterUser([FromBody] RegisterRequestDto model)
    {
        if (!ModelState.IsValid || model is null)
        {
            return BadRequest(new GenericResponseDto<dynamic>
            {
                StatusCode = HttpStatusCode.BadRequest,
                Message = "Invalid registration data."
            });
        }

        var creatorId = _userContextService.LoggedInUser.Id;
        var forbidElevatedUserTypes = !_userContextService.IsDeveloper;

        // Forbid creating Developer/SuperAdmin here
        var response = await _auth.TryRegisterAsync(model, creatorId, forbidElevatedUserTypes).ConfigureAwait(false);
        if (response != null && response.StatusCode == HttpStatusCode.OK)
        {
            // Note: original used MethodNotAllowed inside a BadRequest body; keeping that pattern.
            return CreatedAtAction(nameof(RegisterUser), new GenericResponseDto<TokenResponseDto>
            {
                StatusCode = HttpStatusCode.Created,
                Message = response.Message,
                Response = response.Response
            });
        }
        return BadRequest(response);
    }

    [HttpGet("check-username/{username}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(GenericResponseDto<dynamic>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CheckUserNameAvailability(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return BadRequest(new GenericResponseDto<dynamic>
            {
                StatusCode = HttpStatusCode.BadRequest,
                Message = "username is required."
            });
        }

        var available = await _auth.IsUserNameAvailableAsync(username).ConfigureAwait(false);
        return Ok(new { username, available });
    }
}
