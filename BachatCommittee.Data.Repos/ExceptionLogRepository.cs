// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using awisk.common.Data.Db;
using awisk.common.Data.Db.Interfaces;
using BachatCommittee.Data.Entities;
using BachatCommittee.Data.Repos.Interfaces;

namespace BachatCommittee.Data.Repos;

public class ExceptionLogRepository(IRepositorySettings repositorySettings) : RepositoryBasePostgreSql(repositorySettings.ConnectionString), IExceptionLogRepository
{
    public async Task<IEnumerable<ExceptionLogEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var sql = "SELECT * FROM \"ExceptionLogs\" ORDER BY \"CreatedOn\" DESC;";
        return await QueryAsync<ExceptionLogEntity>(sql, null, System.Data.CommandType.Text).ConfigureAwait(false);
    }

    public async Task<(IEnumerable<ExceptionLogEntity> Items, int TotalCount)> GetAllPagedAsync(
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var offset = (pageNumber - 1) * pageSize;
        var parameters = new { PageSize = pageSize, Offset = offset };

        var sql = @"
            SELECT * FROM ""ExceptionLogs""
            ORDER BY ""CreatedOn"" DESC
            LIMIT @PageSize OFFSET @Offset;
        ";

        var countSql = @"SELECT COUNT(*) FROM ""ExceptionLogs"";";

        var items = await QueryAsync<ExceptionLogEntity>(sql, parameters, System.Data.CommandType.Text).ConfigureAwait(false);
        var totalCount = await QueryFirstOrDefaultAsync<int>(countSql, null, System.Data.CommandType.Text).ConfigureAwait(false);

        return (items, totalCount);
    }
    public async Task<ExceptionLogEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await GetByIdAsync<ExceptionLogEntity, Guid>(id).ConfigureAwait(false);
    }

    public async Task<ExceptionLogEntity> InsertAsync(ExceptionLogEntity entity, CancellationToken cancellationToken = default)
    {
        return await InsertAsync<ExceptionLogEntity>(entity).ConfigureAwait(false);
    }

    public async Task InsertAsync(IEnumerable<ExceptionLogEntity> entities, CancellationToken cancellationToken = default)
    {
        await InsertAsync<ExceptionLogEntity>(entities).ConfigureAwait(false);
    }

    public async Task<ExceptionLogEntity> LogExceptionAsync(Exception ex, string url, CancellationToken cancellationToken = default)
    {
        var log = new ExceptionLogEntity
        {
            Message = ex.Message,
            StackTrace = ex.StackTrace ?? string.Empty,
            Type = ex.GetType().FullName ?? "Unknown",
            URL = url,
            CreatedOn = DateTime.UtcNow
        };

        return await InsertAsync(log, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> UpdateAsync(ExceptionLogEntity entity, CancellationToken cancellationToken = default)
    {
        return await UpdateAsync<ExceptionLogEntity>(entity).ConfigureAwait(false);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (entity == null)
            return false;

        return await DeleteAsync<ExceptionLogEntity>(entity).ConfigureAwait(false);
    }

    public async Task<bool> DeleteAsync(ExceptionLogEntity entity, CancellationToken cancellationToken = default)
    {
        return await DeleteAsync<ExceptionLogEntity>(entity).ConfigureAwait(false);
    }
}
