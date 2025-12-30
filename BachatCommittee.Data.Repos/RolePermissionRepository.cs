// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using awisk.common.Data.Db;
using awisk.common.Data.Db.Interfaces;
using BachatCommittee.Data.Entities;
using BachatCommittee.Data.Repos.Interfaces;

namespace BachatCommittee.Data.Repos;

public class RolePermissionRepository(IRepositorySettings repositorySettings) : RepositoryBasePostgreSql(repositorySettings.ConnectionString), IRolePermissionRepository
{
    public async Task<RolePermissionEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sql = @"
            SELECT * FROM public.""RolePermissions""
            WHERE ""Id"" = @Id
              AND ""IsDeleted"" = false;";
        return await QueryFirstOrDefaultAsync<RolePermissionEntity>(sql, new { Id = id }, System.Data.CommandType.Text).ConfigureAwait(false);
    }

    public async Task<IEnumerable<RolePermissionEntity>> GetByRoleIdAsync(string roleId, CancellationToken cancellationToken = default)
    {
        var sql = @"
            SELECT * FROM public.""RolePermissions""
            WHERE ""RoleId"" = @RoleId
              AND ""IsDeleted"" = false
              AND ""IsActive"" = true;";
        return await QueryAsync<RolePermissionEntity>(sql, new { RoleId = roleId }, System.Data.CommandType.Text).ConfigureAwait(false);
    }

    public async Task<IEnumerable<RolePermissionEntity>> GetByPermissionIdAsync(Guid permissionId, CancellationToken cancellationToken = default)
    {
        var sql = @"
            SELECT * FROM public.""RolePermissions""
            WHERE ""PermissionId"" = @PermissionId
              AND ""IsDeleted"" = false
              AND ""IsActive"" = true;";
        return await QueryAsync<RolePermissionEntity>(sql, new { PermissionId = permissionId }, System.Data.CommandType.Text).ConfigureAwait(false);
    }

    public async Task<RolePermissionEntity?> GetByRoleAndPermissionAsync(string roleId, Guid permissionId, CancellationToken cancellationToken = default)
    {
        var sql = @"
            SELECT * FROM public.""RolePermissions""
            WHERE ""RoleId"" = @RoleId
              AND ""PermissionId"" = @PermissionId
              AND ""IsDeleted"" = false;";
        return await QueryFirstOrDefaultAsync<RolePermissionEntity>(
            sql,
            new { RoleId = roleId, PermissionId = permissionId },
            System.Data.CommandType.Text).ConfigureAwait(false);
    }

    public async Task<RolePermissionEntity> InsertAsync(RolePermissionEntity entity, CancellationToken cancellationToken = default)
    {
        return await InsertAsync<RolePermissionEntity>(entity).ConfigureAwait(false);
    }

    public async Task InsertAsync(IEnumerable<RolePermissionEntity> entities, CancellationToken cancellationToken = default)
    {
        await InsertAsync<RolePermissionEntity>(entities).ConfigureAwait(false);
    }

    public async Task<bool> UpdateAsync(RolePermissionEntity entity, CancellationToken cancellationToken = default)
    {
        entity.ModifiedOn = DateTime.UtcNow;
        return await UpdateAsync<RolePermissionEntity>(entity).ConfigureAwait(false);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sql = @"
            UPDATE public.""RolePermissions""
            SET ""IsDeleted"" = true,
                ""ModifiedOn"" = CURRENT_TIMESTAMP
            WHERE ""Id"" = @Id;";
        var rowsAffected = await ExecuteAsync(sql, new { Id = id }, System.Data.CommandType.Text).ConfigureAwait(false);
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteByRoleAndPermissionAsync(string roleId, Guid permissionId, CancellationToken cancellationToken = default)
    {
        var sql = @"
            UPDATE public.""RolePermissions""
            SET ""IsDeleted"" = true,
                ""ModifiedOn"" = CURRENT_TIMESTAMP
            WHERE ""RoleId"" = @RoleId
              AND ""PermissionId"" = @PermissionId;";
        var rowsAffected = await ExecuteAsync(
            sql,
            new { RoleId = roleId, PermissionId = permissionId },
            System.Data.CommandType.Text).ConfigureAwait(false);
        return rowsAffected > 0;
    }

    public async Task<bool> ExistsAsync(string roleId, Guid permissionId, CancellationToken cancellationToken = default)
    {
        var sql = @"
            SELECT COUNT(1)
            FROM public.""RolePermissions""
            WHERE ""RoleId"" = @RoleId
              AND ""PermissionId"" = @PermissionId
              AND ""IsDeleted"" = false;";
        var count = await QueryFirstOrDefaultAsync<int>(
            sql,
            new { RoleId = roleId, PermissionId = permissionId },
            System.Data.CommandType.Text).ConfigureAwait(false);
        return count > 0;
    }
}

