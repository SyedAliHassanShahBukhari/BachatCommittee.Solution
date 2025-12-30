// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using awisk.common.DTOs.Responses;
using BachatCommittee.Data.Entities;
using BachatCommittee.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BachatCommittee.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class BaseAPIController(IExceptionLogService exceptionLogService) : ControllerBase
{
    protected readonly IExceptionLogService _exceptionLog = exceptionLogService;

    [NonAction]
    protected void GenerateLog(Exception ex, string url)
    {
        // Fire-and-forget async logging to avoid blocking the response
        _ = Task.Run(async () =>
        {
            try
            {
                var entity = new ExceptionLogEntity
                {
                    CreatedOn = DateTime.UtcNow,
                    Message = ex.Message,
                    Type = ex.GetType().Name,
                    StackTrace = ex.StackTrace ?? string.Empty,
                    URL = url
                };
                await _exceptionLog.InsertAsync(entity).ConfigureAwait(false);
            }
            catch
            {
                // Swallow exceptions in logging to prevent logging failures from affecting the request
                // In production, consider logging to a fallback logger (console, file, etc.)
            }
        });
    }

    [NonAction]
    public ObjectResult LogExceptionAndReturnResponse(Exception ex, string url, dynamic? obj = null)
    {
        GenerateLog(ex, url);
        var response = InitObject<dynamic>(HttpStatusCode.InternalServerError);
        response.Message = ex.Message;
        response.Response = obj;
        return StatusCode(StatusCodes.Status500InternalServerError, response);
    }

    [NonAction]
    protected GenericResponseDto<T> InitObject<T>(HttpStatusCode statusCode = HttpStatusCode.BadRequest) where T : class
    {
        return new GenericResponseDto<T>()
        {
            StatusCode = statusCode,
            Message = "",
            Response = null
        };
    }

    [NonAction]
    protected ObjectResult ExceptionResponse(string message)
    {
        return ExceptionResponse<dynamic>(message);
    }

    [NonAction]
    protected ObjectResult ExceptionResponse<T>(string message, T? obj = null) where T : class
    {
        var response = InitObject<T>(HttpStatusCode.InternalServerError);
        response.Message = message;
        response.Response = obj;
        return StatusCode(StatusCodes.Status400BadRequest, response);
    }

    [NonAction]
    protected ObjectResult SuccessResponse(string message)
    {
        return SuccessResponse<dynamic>(message);
    }

    [NonAction]
    protected ObjectResult SuccessResponse<T>(string message, T? obj = null) where T : class
    {
        var response = InitObject<T>(HttpStatusCode.OK);
        response.Message = message;
        response.Response = obj;
        return Ok(response);
    }

    [NonAction]
    protected ObjectResult UnauthorizedResponse()
    {
        return UnauthorizedResponse<dynamic>();
    }

    [NonAction]
    protected ObjectResult UnauthorizedResponse(string message)
    {
        return UnauthorizedResponse<dynamic>(message);
    }

    [NonAction]
    protected ObjectResult UnauthorizedResponse<T>() where T : class
    {
        var response = InitObject<T>(HttpStatusCode.Unauthorized);
        response.Message = "Unauthorized";
        return Unauthorized(response);
    }

    [NonAction]
    protected ObjectResult UnauthorizedResponse<T>(string message) where T : class
    {
        var response = InitObject<T>(HttpStatusCode.Unauthorized);
        response.Message = message;
        return Unauthorized(response);
    }

    [NonAction]
    protected ObjectResult NotFoundResponse(string message = "Not found")
    {
        return NotFoundResponse<dynamic>(message);
    }
    [NonAction]
    protected ObjectResult NotFoundResponse<T>(string message = "Not found") where T : class
    {
        var response = InitObject<T>(HttpStatusCode.NotFound);
        response.Message = message;
        return NotFound(response);
    }
}
