// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BachatCommittee.Data.Entities;

namespace BachatCommittee.Data.Repos.Interfaces;

public interface IRolePermissionRepository
{
    Task<RolePermissionEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<RolePermissionEntity>> GetByRoleIdAsync(string roleId, CancellationToken cancellationToken = default);
    Task<IEnumerable<RolePermissionEntity>> GetByPermissionIdAsync(Guid permissionId, CancellationToken cancellationToken = default);
    Task<RolePermissionEntity?> GetByRoleAndPermissionAsync(string roleId, Guid permissionId, CancellationToken cancellationToken = default);
    Task<RolePermissionEntity> InsertAsync(RolePermissionEntity entity, CancellationToken cancellationToken = default);
    Task InsertAsync(IEnumerable<RolePermissionEntity> entities, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(RolePermissionEntity entity, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> DeleteByRoleAndPermissionAsync(string roleId, Guid permissionId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string roleId, Guid permissionId, CancellationToken cancellationToken = default);
}

