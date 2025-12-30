// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BachatCommittee.Data.Entities;

namespace BachatCommittee.Data.Repos.Interfaces;

public interface IPoolRepository
{
    Task<PoolEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PoolEntity?> GetByCodeAsync(Guid tenantId, string code, CancellationToken cancellationToken = default);
    Task<(IEnumerable<PoolEntity> Items, int TotalCount)> ListAsync(
        Guid tenantId,
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default);
    Task<PoolEntity> InsertAsync(PoolEntity entity, CancellationToken cancellationToken = default);
}
