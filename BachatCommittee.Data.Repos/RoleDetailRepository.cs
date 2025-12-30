// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using awisk.common.Data.Db;
using awisk.common.Data.Db.Interfaces;
using BachatCommittee.Data.Entities;
using BachatCommittee.Data.Repos.Interfaces;

namespace BachatCommittee.Data.Repos;

public class RoleDetailRepository(IRepositorySettings repositorySettings) : RepositoryBasePostgreSql(repositorySettings.ConnectionString), IRoleDetailRepository
{
    public async Task<RoleDetailEntity?> GetByRoleIdAsync(string roleId, CancellationToken cancellationToken = default)
    {
        var sql = @"
            SELECT * FROM public.""RoleDetails""
            WHERE ""RoleId"" = @RoleId
              AND ""IsDeleted"" = false;";
        return await QueryFirstOrDefaultAsync<RoleDetailEntity>(sql, new { RoleId = roleId }, System.Data.CommandType.Text).ConfigureAwait(false);
    }

    public async Task<IEnumerable<RoleDetailEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var sql = @"
            SELECT * FROM public.""RoleDetails""
            WHERE ""IsDeleted"" = false
            ORDER BY ""RoleId"";";
        return await QueryAsync<RoleDetailEntity>(sql, null, System.Data.CommandType.Text).ConfigureAwait(false);
    }

    public async Task<IEnumerable<RoleDetailEntity>> GetPreDefinedRolesAsync(CancellationToken cancellationToken = default)
    {
        var sql = @"
            SELECT * FROM public.""RoleDetails""
            WHERE ""IsPreDefined"" = true
              AND ""IsDeleted"" = false
            ORDER BY ""RoleId"";";
        return await QueryAsync<RoleDetailEntity>(sql, null, System.Data.CommandType.Text).ConfigureAwait(false);
    }

    public async Task<IEnumerable<RoleDetailEntity>> GetSystemRolesAsync(CancellationToken cancellationToken = default)
    {
        var sql = @"
            SELECT * FROM public.""RoleDetails""
            WHERE ""IsSystemRole"" = true
              AND ""IsDeleted"" = false
            ORDER BY ""RoleId"";";
        return await QueryAsync<RoleDetailEntity>(sql, null, System.Data.CommandType.Text).ConfigureAwait(false);
    }

    public async Task<RoleDetailEntity> InsertAsync(RoleDetailEntity entity, CancellationToken cancellationToken = default)
    {
        // RoleDetailEntity uses RoleId as primary key, not Id, so we need custom insert logic
        var sql = @"
            INSERT INTO public.""RoleDetails"" (
                ""RoleId"", ""Description"", ""IsPreDefined"", ""IsSystemRole"",
                ""CreatedBy"", ""CreatedOn"", ""ModifiedBy"", ""ModifiedOn"",
                ""IsDeleted"", ""IsActive""
            )
            VALUES (
                @RoleId, @Description, @IsPreDefined, @IsSystemRole,
                @CreatedBy, @CreatedOn, @ModifiedBy, @ModifiedOn,
                @IsDeleted, @IsActive
            );";

        await ExecuteAsync(sql, entity, System.Data.CommandType.Text).ConfigureAwait(false);
        return entity;
    }

    public async Task<bool> UpdateAsync(RoleDetailEntity entity, CancellationToken cancellationToken = default)
    {
        entity.ModifiedOn = DateTime.UtcNow;
        var sql = @"
            UPDATE public.""RoleDetails""
            SET ""Description"" = @Description,
                ""IsPreDefined"" = @IsPreDefined,
                ""IsSystemRole"" = @IsSystemRole,
                ""ModifiedBy"" = @ModifiedBy,
                ""ModifiedOn"" = @ModifiedOn,
                ""IsDeleted"" = @IsDeleted,
                ""IsActive"" = @IsActive
            WHERE ""RoleId"" = @RoleId;";
        var rowsAffected = await ExecuteAsync(sql, entity, System.Data.CommandType.Text).ConfigureAwait(false);
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(string roleId, CancellationToken cancellationToken = default)
    {
        var sql = @"
            UPDATE public.""RoleDetails""
            SET ""IsDeleted"" = true,
                ""ModifiedOn"" = CURRENT_TIMESTAMP
            WHERE ""RoleId"" = @RoleId;";
        var rowsAffected = await ExecuteAsync(sql, new { RoleId = roleId }, System.Data.CommandType.Text).ConfigureAwait(false);
        return rowsAffected > 0;
    }

    public async Task<bool> ExistsAsync(string roleId, CancellationToken cancellationToken = default)
    {
        var sql = @"
            SELECT COUNT(1)
            FROM public.""RoleDetails""
            WHERE ""RoleId"" = @RoleId
              AND ""IsDeleted"" = false;";
        var count = await QueryFirstOrDefaultAsync<int>(sql, new { RoleId = roleId }, System.Data.CommandType.Text).ConfigureAwait(false);
        return count > 0;
    }
}

