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
[Authorize]
public class PoolsController(IExceptionLogService exceptionLogService, IPoolService poolService) : BaseAPIController(exceptionLogService)
{
    private readonly IPoolService _poolService = poolService;

    [HttpGet]
    [ProducesResponseType(typeof(GenericResponseDto<PagedResponseDto<PoolResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync([FromQuery] Guid tenantId, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, [FromQuery] string? search = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _poolService.ListAsync(tenantId, page, pageSize, search, cancellationToken).ConfigureAwait(false);
            return Ok(new GenericResponseDto<PagedResponseDto<PoolResponseDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Message = "Pools retrieved successfully.",
                Response = result
            });
        }
        catch (Exception ex)
        {
            return LogExceptionAndReturnResponse(ex, Request.Path);
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GenericResponseDto<PoolResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(GenericResponseDto<dynamic>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var pool = await _poolService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (pool == null)
            {
                return NotFound(new GenericResponseDto<dynamic>
                {
                    StatusCode = HttpStatusCode.NotFound,
                    Message = "Pool not found."
                });
            }

            return Ok(new GenericResponseDto<PoolResponseDto>
            {
                StatusCode = HttpStatusCode.OK,
                Message = "Pool retrieved successfully.",
                Response = pool
            });
        }
        catch (Exception ex)
        {
            return LogExceptionAndReturnResponse(ex, Request.Path);
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(GenericResponseDto<PoolResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(GenericResponseDto<dynamic>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAsync([FromBody] CreatePoolRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid || request == null)
            {
                return BadRequest(new GenericResponseDto<dynamic>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Message = "Invalid pool request payload."
                });
            }

            var created = await _poolService.CreateAsync(request, cancellationToken).ConfigureAwait(false);
            return CreatedAtAction(nameof(GetByIdAsync), new { id = created.Id }, new GenericResponseDto<PoolResponseDto>
            {
                StatusCode = HttpStatusCode.Created,
                Message = "Pool created successfully.",
                Response = created
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
}
