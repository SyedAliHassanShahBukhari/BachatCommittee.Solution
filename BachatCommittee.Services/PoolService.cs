// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using BachatCommittee.Data.Entities;
using BachatCommittee.Data.Repos.Interfaces;
using BachatCommittee.Models.DTOs.Requests;
using BachatCommittee.Models.DTOs.Responses;
using BachatCommittee.Services.Interfaces;

namespace BachatCommittee.Services;

public class PoolService(IPoolRepository poolRepository) : IPoolService
{
    private readonly IPoolRepository _poolRepository = poolRepository;

    public async Task<PoolResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _poolRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return entity == null ? null : MapToResponse(entity);
    }

    public async Task<PagedResponseDto<PoolResponseDto>> ListAsync(
        Guid tenantId,
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var (items, totalCount) = await _poolRepository.ListAsync(tenantId, page, pageSize, search, cancellationToken)
            .ConfigureAwait(false);

        var mapped = items.Select(MapToResponse).ToList();
        return new PagedResponseDto<PoolResponseDto>(mapped, totalCount, page, pageSize);
    }

    public async Task<PoolResponseDto> CreateAsync(CreatePoolRequestDto request, CancellationToken cancellationToken = default)
    {
        var existing = await _poolRepository.GetByCodeAsync(request.TenantId, request.Code, cancellationToken)
            .ConfigureAwait(false);
        if (existing != null)
        {
            throw new InvalidOperationException($"A pool with code '{request.Code}' already exists for this tenant.");
        }

        var entity = new PoolEntity
        {
            TenantId = request.TenantId,
            Name = request.Name.Trim(),
            Code = request.Code.Trim(),
            TimeZone = string.IsNullOrWhiteSpace(request.TimeZone) ? null : request.TimeZone.Trim()
        };

        var inserted = await _poolRepository.InsertAsync(entity, cancellationToken).ConfigureAwait(false);
        return MapToResponse(inserted);
    }

    private static PoolResponseDto MapToResponse(PoolEntity entity)
    {
        return new PoolResponseDto
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            Name = entity.Name,
            Code = entity.Code,
            TimeZone = entity.TimeZone,
            IsActive = entity.IsActive,
            CreatedOn = entity.CreatedOn
        };
    }
}
