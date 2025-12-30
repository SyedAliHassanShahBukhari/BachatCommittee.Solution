// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BachatCommittee.Data.Entities;

namespace BachatCommittee.Data.Repos.Interfaces;

public interface IActionRepository
{
    Task<ActionEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ActionEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<ActionEntity>> GetByControllerAsync(string controllerName, CancellationToken cancellationToken = default);
    Task<ActionEntity?> GetByControllerAndActionAsync(string controllerName, string actionName, string httpMethod, CancellationToken cancellationToken = default);
    Task<ActionEntity> InsertAsync(ActionEntity entity, CancellationToken cancellationToken = default);
    Task InsertAsync(IEnumerable<ActionEntity> entities, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(ActionEntity entity, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string controllerName, string actionName, string httpMethod, CancellationToken cancellationToken = default);
}

