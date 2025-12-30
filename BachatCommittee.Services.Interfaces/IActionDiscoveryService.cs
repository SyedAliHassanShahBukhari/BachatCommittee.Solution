// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BachatCommittee.Services.Interfaces;

/// <summary>
/// Service interface for discovering and registering controller actions.
/// </summary>
public interface IActionDiscoveryService
{
    /// <summary>
    /// Discovers all actions in controllers and registers them in the database.
    /// </summary>
    Task DiscoverAndRegisterActionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the list of discovered actions.
    /// </summary>
    Task<List<ActionInfo>> GetDiscoveredActionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Synchronizes actions with the database (adds new, marks removed as inactive).
    /// </summary>
    Task SyncActionsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents information about a discovered action.
/// </summary>
public class ActionInfo
{
    public string ControllerName { get; set; } = string.Empty;
    public string ActionName { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public string? Route { get; set; }
    public string? Description { get; set; }
}

