// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BachatCommittee.Data.Entities;

namespace BachatCommittee.Data.Repos.Interfaces;

public interface IUserPermissionRepository
{
    Task<UserPermissionEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserPermissionEntity>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserPermissionEntity>> GetActivePermissionsByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserPermissionEntity>> GetByPermissionIdAsync(Guid permissionId, CancellationToken cancellationToken = default);
    Task<UserPermissionEntity?> GetByUserAndPermissionAsync(string userId, Guid permissionId, CancellationToken cancellationToken = default);
    Task<UserPermissionEntity> InsertAsync(UserPermissionEntity entity, CancellationToken cancellationToken = default);
    Task InsertAsync(IEnumerable<UserPermissionEntity> entities, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(UserPermissionEntity entity, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> DeleteByUserAndPermissionAsync(string userId, Guid permissionId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string userId, Guid permissionId, CancellationToken cancellationToken = default);
    Task RevokePermissionAsync(string userId, Guid permissionId, Guid revokedBy, CancellationToken cancellationToken = default);
}

