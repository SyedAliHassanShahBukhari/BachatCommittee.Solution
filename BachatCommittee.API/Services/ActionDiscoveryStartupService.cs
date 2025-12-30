// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BachatCommittee.Services.Interfaces;
using Npgsql;

namespace BachatCommittee.API.Services;

/// <summary>
/// Hosted service that runs action discovery on application startup.
/// </summary>
public class ActionDiscoveryStartupService(IServiceProvider serviceProvider, ILogger<ActionDiscoveryStartupService> logger) : IHostedService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<ActionDiscoveryStartupService> _logger = logger;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting action discovery on application startup...");

            // Wait a bit to ensure all assemblies are loaded
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);

            using var scope = _serviceProvider.CreateScope();
            var actionDiscoveryService = scope.ServiceProvider.GetRequiredService<IActionDiscoveryService>();

            // Log how many actions were discovered
            var discoveredActions = await actionDiscoveryService.GetDiscoveredActionsAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Found {Count} existing actions in database before discovery", discoveredActions.Count);

            await actionDiscoveryService.DiscoverAndRegisterActionsAsync(cancellationToken).ConfigureAwait(false);

            // Log how many actions exist after discovery
            var actionsAfter = await actionDiscoveryService.GetDiscoveredActionsAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Action discovery completed successfully. Total actions in database: {Count}", actionsAfter.Count);
        }
        catch (PostgresException pgEx) when (pgEx.SqlState == "42P01")
        {
            // Table does not exist - migrations haven't been run
            _logger.LogWarning(pgEx,
                "Permission system tables do not exist. Please run FluentMigrator migrations first. " +
                "Run: dotnet run --project BachatCommittee.Data.Migrator\\BachatCommittee.Data.Migrator.csproj -- localhost SampleDb -u postgres -p sunbonn " +
                "Actions can still be discovered manually via the /api/v1/permissions/sync endpoint after migrations are complete.");
        }
        catch (Exception ex)
        {
            // Log error but don't fail startup - action discovery can be run manually via API
            _logger.LogError(ex, "Error during action discovery on startup. Actions can still be discovered manually via the /api/v1/permissions/sync endpoint.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

