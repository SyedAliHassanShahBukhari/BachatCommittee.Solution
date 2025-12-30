// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using awisk.common.Data.Db.Interfaces;
using BachatCommittee.Data.Entities;
using BachatCommittee.Data.Repos.Base;
using BachatCommittee.Data.Repos.Interfaces;

namespace BachatCommittee.Data.Repos;

public class PoolRepository(IRepositorySettings repositorySettings)
    : RepositoryBasePostgreSqlOptimized(repositorySettings.ConnectionString), IPoolRepository
{
    public async Task<PoolEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = @"SELECT * FROM public.""Pools"" WHERE ""Id"" = @Id AND ""IsDeleted"" = false;";
        return await QueryFirstOrDefaultAsync<PoolEntity>(sql, new { Id = id }, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<PoolEntity?> GetByCodeAsync(Guid tenantId, string code, CancellationToken cancellationToken = default)
    {
        const string sql = @"SELECT * FROM public.""Pools"" WHERE ""TenantId"" = @TenantId AND ""Code"" = @Code AND ""IsDeleted"" = false;";
        return await QueryFirstOrDefaultAsync<PoolEntity>(sql, new { TenantId = tenantId, Code = code }, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<(IEnumerable<PoolEntity> Items, int TotalCount)> ListAsync(
        Guid tenantId,
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var offset = (page - 1) * pageSize;
        var filters = new List<string>
        {
            "\"TenantId\" = @TenantId",
            "\"IsDeleted\" = false"
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            filters.Add("(\"Name\" ILIKE @Search OR \"Code\" ILIKE @Search)");
        }

        var whereClause = string.Join(" AND ", filters.Select(f => $"({f})"));

        var dataSql = $@"SELECT * FROM public.""Pools"" WHERE {whereClause} ORDER BY ""CreatedOn"" DESC LIMIT @PageSize OFFSET @Offset;";
        var countSql = $@"SELECT COUNT(1) FROM public.""Pools"" WHERE {whereClause};";

        var parameters = new
        {
            TenantId = tenantId,
            Search = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%",
            PageSize = pageSize,
            Offset = offset
        };

        var itemsTask = QueryAsync<PoolEntity>(dataSql, parameters, cancellationToken: cancellationToken);
        var countTask = QuerySingleAsync<int>(countSql, parameters, cancellationToken: cancellationToken);

        await Task.WhenAll(itemsTask, countTask).ConfigureAwait(false);

        return (itemsTask.Result, countTask.Result);
    }

    public async Task<PoolEntity> InsertAsync(PoolEntity entity, CancellationToken cancellationToken = default)
    {
        entity.CreatedOn = DateTime.UtcNow;
        return await InsertAsync<PoolEntity>(entity, cancellationToken).ConfigureAwait(false);
    }
}
