// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BachatCommittee.Models.DTOs.Requests;
using BachatCommittee.Models.DTOs.Responses;

namespace BachatCommittee.Services.Interfaces;

public interface IPoolService
{
    Task<PoolResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResponseDto<PoolResponseDto>> ListAsync(Guid tenantId, int page, int pageSize, string? search, CancellationToken cancellationToken = default);
    Task<PoolResponseDto> CreateAsync(CreatePoolRequestDto request, CancellationToken cancellationToken = default);
}
