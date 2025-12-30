// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BachatCommittee.Data.Entities;

namespace BachatCommittee.Services.Interfaces;

public interface IExceptionLogService
{
    /// <summary>
    /// Gets all exception logs
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of exception log entities</returns>
    Task<IEnumerable<ExceptionLogEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated exception logs
    /// </summary>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple containing items and total count</returns>
    Task<(IEnumerable<ExceptionLogEntity> Items, int TotalCount)> GetAllPagedAsync(
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an exception log by ID
    /// </summary>
    /// <param name="id">The log ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The exception log entity, or null if not found</returns>
    Task<ExceptionLogEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts a new exception log
    /// </summary>
    /// <param name="entity">The exception log entity to insert</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The inserted exception log entity</returns>
    Task<ExceptionLogEntity> InsertAsync(ExceptionLogEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts multiple exception logs
    /// </summary>
    /// <param name="entities">The collection of exception log entities to insert</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task InsertAsync(IEnumerable<ExceptionLogEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs an exception with the provided URL
    /// </summary>
    /// <param name="ex">The exception to log</param>
    /// <param name="url">The URL where the exception occurred</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created exception log entity</returns>
    Task<ExceptionLogEntity> LogExceptionAsync(Exception ex, string url, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing exception log
    /// </summary>
    /// <param name="entity">The exception log entity to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if update was successful, false otherwise</returns>
    Task<bool> UpdateAsync(ExceptionLogEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an exception log by ID
    /// </summary>
    /// <param name="id">The log ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deletion was successful, false otherwise</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an exception log entity
    /// </summary>
    /// <param name="entity">The exception log entity to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deletion was successful, false otherwise</returns>
    Task<bool> DeleteAsync(ExceptionLogEntity entity, CancellationToken cancellationToken = default);
}
