// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using awisk.common.Data.Db;
using awisk.common.Data.Db.Interfaces;
using BachatCommittee.Data.Entities;
using BachatCommittee.Data.Repos.Interfaces;

namespace BachatCommittee.Data.Repos;

public class ActionRepository(IRepositorySettings repositorySettings) : RepositoryBasePostgreSql(repositorySettings.ConnectionString), IActionRepository
{
    public async Task<ActionEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sql = @"
            SELECT * FROM public.""Actions""
            WHERE ""Id"" = @Id
              AND ""IsDeleted"" = false;";
        return await QueryFirstOrDefaultAsync<ActionEntity>(sql, new { Id = id }, System.Data.CommandType.Text).ConfigureAwait(false);
    }

    public async Task<IEnumerable<ActionEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var sql = @"
            SELECT * FROM public.""Actions""
            WHERE ""IsDeleted"" = false
            ORDER BY ""ControllerName"", ""ActionName"";";
        return await QueryAsync<ActionEntity>(sql, null, System.Data.CommandType.Text).ConfigureAwait(false);
    }

    public async Task<IEnumerable<ActionEntity>> GetByControllerAsync(string controllerName, CancellationToken cancellationToken = default)
    {
        var sql = @"
            SELECT * FROM public.""Actions""
            WHERE ""ControllerName"" = @ControllerName
              AND ""IsDeleted"" = false
            ORDER BY ""ActionName"";";
        return await QueryAsync<ActionEntity>(sql, new { ControllerName = controllerName }, System.Data.CommandType.Text).ConfigureAwait(false);
    }

    public async Task<ActionEntity?> GetByControllerAndActionAsync(string controllerName, string actionName, string httpMethod, CancellationToken cancellationToken = default)
    {
        var sql = @"
            SELECT * FROM public.""Actions""
            WHERE ""ControllerName"" = @ControllerName
              AND ""ActionName"" = @ActionName
              AND ""HttpMethod"" = @HttpMethod
              AND ""IsDeleted"" = false;";
        return await QueryFirstOrDefaultAsync<ActionEntity>(
            sql,
            new { ControllerName = controllerName, ActionName = actionName, HttpMethod = httpMethod },
            System.Data.CommandType.Text).ConfigureAwait(false);
    }

    public async Task<ActionEntity> InsertAsync(ActionEntity entity, CancellationToken cancellationToken = default)
    {
        return await InsertAsync<ActionEntity>(entity).ConfigureAwait(false);
    }

    public async Task InsertAsync(IEnumerable<ActionEntity> entities, CancellationToken cancellationToken = default)
    {
        await InsertAsync<ActionEntity>(entities).ConfigureAwait(false);
    }

    public async Task<bool> UpdateAsync(ActionEntity entity, CancellationToken cancellationToken = default)
    {
        entity.ModifiedOn = DateTime.UtcNow;
        return await UpdateAsync<ActionEntity>(entity).ConfigureAwait(false);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sql = @"
            UPDATE public.""Actions""
            SET ""IsDeleted"" = true,
                ""ModifiedOn"" = CURRENT_TIMESTAMP
            WHERE ""Id"" = @Id;";
        var rowsAffected = await ExecuteAsync(sql, new { Id = id }, System.Data.CommandType.Text).ConfigureAwait(false);
        return rowsAffected > 0;
    }

    public async Task<bool> ExistsAsync(string controllerName, string actionName, string httpMethod, CancellationToken cancellationToken = default)
    {
        var sql = @"
            SELECT COUNT(1)
            FROM public.""Actions""
            WHERE ""ControllerName"" = @ControllerName
              AND ""ActionName"" = @ActionName
              AND ""HttpMethod"" = @HttpMethod
              AND ""IsDeleted"" = false;";
        var count = await QueryFirstOrDefaultAsync<int>(
            sql,
            new { ControllerName = controllerName, ActionName = actionName, HttpMethod = httpMethod },
            System.Data.CommandType.Text).ConfigureAwait(false);
        return count > 0;
    }
}

