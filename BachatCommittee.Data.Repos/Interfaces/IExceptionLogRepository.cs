// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BachatCommittee.Data.Entities;

namespace BachatCommittee.Data.Repos.Interfaces;

public interface IExceptionLogRepository
{
    Task<IEnumerable<ExceptionLogEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<(IEnumerable<ExceptionLogEntity> Items, int TotalCount)> GetAllPagedAsync(
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);
    Task<ExceptionLogEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ExceptionLogEntity> InsertAsync(ExceptionLogEntity entity, CancellationToken cancellationToken = default);
    Task InsertAsync(IEnumerable<ExceptionLogEntity> entities, CancellationToken cancellationToken = default);
    Task<ExceptionLogEntity> LogExceptionAsync(Exception ex, string url, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(ExceptionLogEntity entity, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(ExceptionLogEntity entity, CancellationToken cancellationToken = default);
}

