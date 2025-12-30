// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BachatCommittee.Data.Entities;

namespace BachatCommittee.Data.Repos.Interfaces;

public interface IRoleDetailRepository
{
    Task<RoleDetailEntity?> GetByRoleIdAsync(string roleId, CancellationToken cancellationToken = default);
    Task<IEnumerable<RoleDetailEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<RoleDetailEntity>> GetPreDefinedRolesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<RoleDetailEntity>> GetSystemRolesAsync(CancellationToken cancellationToken = default);
    Task<RoleDetailEntity> InsertAsync(RoleDetailEntity entity, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(RoleDetailEntity entity, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string roleId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string roleId, CancellationToken cancellationToken = default);
}

