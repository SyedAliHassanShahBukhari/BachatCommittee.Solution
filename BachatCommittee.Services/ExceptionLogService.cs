// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BachatCommittee.Data.Entities;
using BachatCommittee.Data.Repos.Interfaces;
using BachatCommittee.Services.Interfaces;

namespace BachatCommittee.Services;

public class ExceptionLogService(IExceptionLogRepository repository) : IExceptionLogService
{
    public async Task<IEnumerable<ExceptionLogEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await repository.GetAllAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<(IEnumerable<ExceptionLogEntity> Items, int TotalCount)> GetAllPagedAsync(
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        return await repository.GetAllPagedAsync(pageNumber, pageSize, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ExceptionLogEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ExceptionLogEntity> InsertAsync(ExceptionLogEntity entity, CancellationToken cancellationToken = default)
    {
        return await repository.InsertAsync(entity, cancellationToken).ConfigureAwait(false);
    }

    public async Task InsertAsync(IEnumerable<ExceptionLogEntity> entities, CancellationToken cancellationToken = default)
    {
        await repository.InsertAsync(entities, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ExceptionLogEntity> LogExceptionAsync(Exception ex, string url, CancellationToken cancellationToken = default)
    {
        return await repository.LogExceptionAsync(ex, url, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> UpdateAsync(ExceptionLogEntity entity, CancellationToken cancellationToken = default)
    {
        return await repository.UpdateAsync(entity, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await repository.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> DeleteAsync(ExceptionLogEntity entity, CancellationToken cancellationToken = default)
    {
        return await repository.DeleteAsync(entity, cancellationToken).ConfigureAwait(false);
    }
}
