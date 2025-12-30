// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using awisk.common.Data.Db;
using awisk.common.Data.Db.Interfaces;
using BachatCommittee.Data.Entities;
using BachatCommittee.Data.Repos.Interfaces;

namespace BachatCommittee.Data.Repos;

public class PermissionRepository(IRepositorySettings repositorySettings) : RepositoryBasePostgreSql(repositorySettings.ConnectionString), IPermissionRepository
{
    public async Task<PermissionEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sql = @"
            SELECT * FROM public.""Permissions""
            WHERE ""Id"" = @Id
              AND ""IsDeleted"" = false;";
        return await QueryFirstOrDefaultAsync<PermissionEntity>(sql, new { Id = id }, System.Data.CommandType.Text).ConfigureAwait(false);
    }

    public async Task<PermissionEntity?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var sql = @"
            SELECT * FROM public.""Permissions""
            WHERE ""Name"" = @Name
              AND ""IsDeleted"" = false;";
        return await QueryFirstOrDefaultAsync<PermissionEntity>(sql, new { Name = name }, System.Data.CommandType.Text).ConfigureAwait(false);
    }

    public async Task<IEnumerable<PermissionEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var sql = @"
            SELECT * FROM public.""Permissions""
            WHERE ""IsDeleted"" = false
            ORDER BY ""Category"", ""Name"";";
        return await QueryAsync<PermissionEntity>(sql, null, System.Data.CommandType.Text).ConfigureAwait(false);
    }

    public async Task<IEnumerable<PermissionEntity>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        var sql = @"
            SELECT * FROM public.""Permissions""
            WHERE ""Category"" = @Category
              AND ""IsDeleted"" = false
            ORDER BY ""Name"";";
        return await QueryAsync<PermissionEntity>(sql, new { Category = category }, System.Data.CommandType.Text).ConfigureAwait(false);
    }

    public async Task<IEnumerable<PermissionEntity>> GetByActionIdAsync(Guid actionId, CancellationToken cancellationToken = default)
    {
        var sql = @"
            SELECT * FROM public.""Permissions""
            WHERE ""ActionId"" = @ActionId
              AND ""IsDeleted"" = false;";
        return await QueryAsync<PermissionEntity>(sql, new { ActionId = actionId }, System.Data.CommandType.Text).ConfigureAwait(false);
    }

    public async Task<PermissionEntity> InsertAsync(PermissionEntity entity, CancellationToken cancellationToken = default)
    {
        return await InsertAsync<PermissionEntity>(entity).ConfigureAwait(false);
    }

    public async Task InsertAsync(IEnumerable<PermissionEntity> entities, CancellationToken cancellationToken = default)
    {
        await InsertAsync<PermissionEntity>(entities).ConfigureAwait(false);
    }

    public async Task<bool> UpdateAsync(PermissionEntity entity, CancellationToken cancellationToken = default)
    {
        entity.ModifiedOn = DateTime.UtcNow;
        return await UpdateAsync<PermissionEntity>(entity).ConfigureAwait(false);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sql = @"
            UPDATE public.""Permissions""
            SET ""IsDeleted"" = true,
                ""ModifiedOn"" = CURRENT_TIMESTAMP
            WHERE ""Id"" = @Id;";
        var rowsAffected = await ExecuteAsync(sql, new { Id = id }, System.Data.CommandType.Text).ConfigureAwait(false);
        return rowsAffected > 0;
    }

    public async Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        var sql = @"
            SELECT COUNT(1)
            FROM public.""Permissions""
            WHERE ""Name"" = @Name
              AND ""IsDeleted"" = false;";
        var count = await QueryFirstOrDefaultAsync<int>(sql, new { Name = name }, System.Data.CommandType.Text).ConfigureAwait(false);
        return count > 0;
    }
}

