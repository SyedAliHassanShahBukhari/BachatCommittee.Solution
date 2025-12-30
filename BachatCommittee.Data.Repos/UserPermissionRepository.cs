// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using awisk.common.Data.Db;
using awisk.common.Data.Db.Interfaces;
using BachatCommittee.Data.Entities;
using BachatCommittee.Data.Repos.Base;
using BachatCommittee.Data.Repos.Interfaces;

namespace BachatCommittee.Data.Repos;

public class UserPermissionRepository(IRepositorySettings repositorySettings) : RepositoryBasePostgreSql(repositorySettings.ConnectionString), IUserPermissionRepository
{
    public async Task<UserPermissionEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sql = @"
            SELECT * FROM public.""UserPermissions""
            WHERE ""Id"" = @Id
              AND ""IsDeleted"" = false;";
        return await QueryFirstOrDefaultAsync<UserPermissionEntity>(sql, new { Id = id }, System.Data.CommandType.Text).ConfigureAwait(false);
    }

    public async Task<IEnumerable<UserPermissionEntity>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var sql = @"
            SELECT * FROM public.""UserPermissions""
            WHERE ""UserId"" = @UserId
              AND ""IsDeleted"" = false;";
        return await QueryAsync<UserPermissionEntity>(sql, new { UserId = userId }, System.Data.CommandType.Text).ConfigureAwait(false);
    }

    public async Task<IEnumerable<UserPermissionEntity>> GetActivePermissionsByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var sql = @"
            SELECT * FROM public.""UserPermissions""
            WHERE ""UserId"" = @UserId
              AND ""IsDeleted"" = false
              AND ""IsActive"" = true
              AND ""IsRevoked"" = false
              AND (""ExpiresOn"" IS NULL OR ""ExpiresOn"" > CURRENT_TIMESTAMP);";
        return await QueryAsync<UserPermissionEntity>(sql, new { UserId = userId }, System.Data.CommandType.Text).ConfigureAwait(false);
    }

    public async Task<IEnumerable<UserPermissionEntity>> GetByPermissionIdAsync(Guid permissionId, CancellationToken cancellationToken = default)
    {
        var sql = @"
            SELECT * FROM public.""UserPermissions""
            WHERE ""PermissionId"" = @PermissionId
              AND ""IsDeleted"" = false;";
        return await QueryAsync<UserPermissionEntity>(sql, new { PermissionId = permissionId }, System.Data.CommandType.Text).ConfigureAwait(false);
    }

    public async Task<UserPermissionEntity?> GetByUserAndPermissionAsync(string userId, Guid permissionId, CancellationToken cancellationToken = default)
    {
        var sql = @"
            SELECT * FROM public.""UserPermissions""
            WHERE ""UserId"" = @UserId
              AND ""PermissionId"" = @PermissionId
              AND ""IsDeleted"" = false;";
        return await QueryFirstOrDefaultAsync<UserPermissionEntity>(
            sql,
            new { UserId = userId, PermissionId = permissionId },
            System.Data.CommandType.Text).ConfigureAwait(false);
    }

    public async Task<UserPermissionEntity> InsertAsync(UserPermissionEntity entity, CancellationToken cancellationToken = default)
    {
        return await InsertAsync<UserPermissionEntity>(entity).ConfigureAwait(false);
    }

    public async Task InsertAsync(IEnumerable<UserPermissionEntity> entities, CancellationToken cancellationToken = default)
    {
        await InsertAsync<UserPermissionEntity>(entities).ConfigureAwait(false);
    }

    public async Task<bool> UpdateAsync(UserPermissionEntity entity, CancellationToken cancellationToken = default)
    {
        entity.ModifiedOn = DateTime.UtcNow;
        return await UpdateAsync<UserPermissionEntity>(entity).ConfigureAwait(false);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sql = @"
            UPDATE public.""UserPermissions""
            SET ""IsDeleted"" = true,
                ""ModifiedOn"" = CURRENT_TIMESTAMP
            WHERE ""Id"" = @Id;";
        var rowsAffected = await ExecuteAsync(sql, new { Id = id }, System.Data.CommandType.Text).ConfigureAwait(false);
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteByUserAndPermissionAsync(string userId, Guid permissionId, CancellationToken cancellationToken = default)
    {
        var sql = @"
            UPDATE public.""UserPermissions""
            SET ""IsDeleted"" = true,
                ""ModifiedOn"" = CURRENT_TIMESTAMP
            WHERE ""UserId"" = @UserId
              AND ""PermissionId"" = @PermissionId;";
        var rowsAffected = await ExecuteAsync(
            sql,
            new { UserId = userId, PermissionId = permissionId },
            System.Data.CommandType.Text).ConfigureAwait(false);
        return rowsAffected > 0;
    }

    public async Task<bool> ExistsAsync(string userId, Guid permissionId, CancellationToken cancellationToken = default)
    {
        var sql = @"
            SELECT COUNT(1)
            FROM public.""UserPermissions""
            WHERE ""UserId"" = @UserId
              AND ""PermissionId"" = @PermissionId
              AND ""IsDeleted"" = false;";
        var count = await QueryFirstOrDefaultAsync<int>(
            sql,
            new { UserId = userId, PermissionId = permissionId },
            System.Data.CommandType.Text).ConfigureAwait(false);
        return count > 0;
    }

    public async Task RevokePermissionAsync(string userId, Guid permissionId, Guid revokedBy, CancellationToken cancellationToken = default)
    {
        var sql = @"
            UPDATE public.""UserPermissions""
            SET ""IsRevoked"" = true,
                ""RevokedOn"" = CURRENT_TIMESTAMP,
                ""RevokedBy"" = @RevokedBy,
                ""ModifiedOn"" = CURRENT_TIMESTAMP
            WHERE ""UserId"" = @UserId
              AND ""PermissionId"" = @PermissionId
              AND ""IsDeleted"" = false;";
        await ExecuteAsync(
            sql,
            new { UserId = userId, PermissionId = permissionId, RevokedBy = revokedBy },
            System.Data.CommandType.Text).ConfigureAwait(false);
    }
}

