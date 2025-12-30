// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BachatCommittee.Data.Entities;

namespace BachatCommittee.Data.Repos.Interfaces;

public interface IPermissionRepository
{
    Task<PermissionEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PermissionEntity?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IEnumerable<PermissionEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<PermissionEntity>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);
    Task<IEnumerable<PermissionEntity>> GetByActionIdAsync(Guid actionId, CancellationToken cancellationToken = default);
    Task<PermissionEntity> InsertAsync(PermissionEntity entity, CancellationToken cancellationToken = default);
    Task InsertAsync(IEnumerable<PermissionEntity> entities, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(PermissionEntity entity, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default);
}

