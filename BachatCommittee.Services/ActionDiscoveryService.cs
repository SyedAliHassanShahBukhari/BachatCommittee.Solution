// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using BachatCommittee.Data.Entities;
using BachatCommittee.Data.Repos.Interfaces;
using BachatCommittee.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace BachatCommittee.Services;

/// <summary>
/// Service for discovering and registering controller actions.
/// </summary>
public class ActionDiscoveryService(
    IActionRepository actionRepository,
    IPermissionRepository permissionRepository) : IActionDiscoveryService
{
    private readonly IActionRepository _actionRepository = actionRepository;
    private readonly IPermissionRepository _permissionRepository = permissionRepository;

    public async Task DiscoverAndRegisterActionsAsync(CancellationToken cancellationToken = default)
    {
        var actionInfos = DiscoverActions();
        if (actionInfos.Count == 0)
        {
            // Log warning if no actions discovered
            System.Diagnostics.Debug.WriteLine("WARNING: No actions discovered during action discovery.");
        }
        await RegisterActionsAsync(actionInfos, cancellationToken).ConfigureAwait(false);
    }

    public async Task<List<ActionInfo>> GetDiscoveredActionsAsync(CancellationToken cancellationToken = default)
    {
        var actions = await _actionRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return [.. actions.Select(a => new ActionInfo
        {
            ControllerName = a.ControllerName,
            ActionName = a.ActionName,
            HttpMethod = a.HttpMethod,
            Route = a.Route,
            Description = a.Description
        })];
    }

    public async Task SyncActionsAsync(CancellationToken cancellationToken = default)
    {
        var discoveredActions = DiscoverActions();
        var existingActions = (await _actionRepository.GetAllAsync(cancellationToken).ConfigureAwait(false)).ToList();

        var createdBy = Guid.Empty; // System user - should be set appropriately

        // Find new actions to add
        foreach (var discovered in discoveredActions)
        {
            var exists = existingActions.Any(e =>
                e.ControllerName == discovered.ControllerName &&
                e.ActionName == discovered.ActionName &&
                e.HttpMethod == discovered.HttpMethod &&
                !e.IsDeleted);

            if (!exists)
            {
                var actionEntity = new ActionEntity
                {
                    ControllerName = discovered.ControllerName,
                    ActionName = discovered.ActionName,
                    HttpMethod = discovered.HttpMethod,
                    Route = discovered.Route,
                    Description = discovered.Description,
                    CreatedBy = createdBy,
                    CreatedOn = DateTime.UtcNow,
                    IsActive = true,
                    IsDeleted = false
                };

                await _actionRepository.InsertAsync(actionEntity, cancellationToken).ConfigureAwait(false);
            }
        }

        // Mark removed actions as inactive (don't delete for audit)
        foreach (var existing in existingActions.Where(e => e.IsActive && !e.IsDeleted))
        {
            var stillExists = discoveredActions.Any(d =>
                d.ControllerName == existing.ControllerName &&
                d.ActionName == existing.ActionName &&
                d.HttpMethod == existing.HttpMethod);

            if (!stillExists)
            {
                existing.IsActive = false;
                existing.ModifiedOn = DateTime.UtcNow;
                await _actionRepository.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static List<ActionInfo> DiscoverActions()
    {
        var actionInfos = new List<ActionInfo>();

        // Get all loaded assemblies and find controllers
        // Controllers are typically in the API assembly
        var assembliesList = AppDomain.CurrentDomain.GetAssemblies().ToList();
        var allControllerTypes = new List<Type>();

        // Prioritize the entry assembly (should be the API assembly)
        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly != null)
        {
            // Ensure entry assembly is in the list (prioritize it)
            if (!assembliesList.Any(a => a.FullName == entryAssembly.FullName))
            {
                assembliesList.Insert(0, entryAssembly); // Add at the beginning
            }
            else
            {
                // Move to front if already in list
                assembliesList.Remove(entryAssembly);
                assembliesList.Insert(0, entryAssembly);
            }
        }

        // Also try to find API assembly by name pattern
        var apiAssembly = assembliesList.FirstOrDefault(a =>
            a.GetName().Name?.Contains("BachatCommittee.API", StringComparison.OrdinalIgnoreCase) == true);

        if (apiAssembly != null && apiAssembly != entryAssembly)
        {
            // Ensure API assembly is also checked
            if (!assembliesList.Contains(apiAssembly))
            {
                assembliesList.Insert(0, apiAssembly);
            }
        }

        foreach (var assembly in assembliesList)
        {
            try
            {
                var _controllers = assembly.GetTypes()
                    .Where(t => typeof(ControllerBase).IsAssignableFrom(t) &&
                               !t.IsAbstract &&
                               t != typeof(ControllerBase))
                    .ToList();

                if (_controllers.Count > 0)
                {
                    allControllerTypes.AddRange(_controllers);
                }
            }
            catch (ReflectionTypeLoadException)
            {
                // Skip assemblies that can't be loaded
                continue;
            }
        }

        var controllers = allControllerTypes;

        foreach (var controller in controllers)
        {
            // Get first route attribute if multiple exist
            var controllerRouteAttrs = controller.GetCustomAttributes<RouteAttribute>(inherit: false).ToList();
            var controllerRouteAttr = controllerRouteAttrs.FirstOrDefault();
            var controllerRoute = controllerRouteAttr?.Template ?? string.Empty;

            // Remove "Controller" suffix from controller name
            var controllerName = controller.Name;
            if (controllerName.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
            {
                controllerName = controllerName[..^"Controller".Length];
            }

            var methods = controller.GetMethods(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName &&
                           m.DeclaringType == controller &&
                           !m.GetCustomAttributes<NonActionAttribute>(inherit: false).Any())
                .ToList();

            foreach (var method in methods)
            {
                var httpMethod = GetHttpMethod(method);
                if (string.IsNullOrEmpty(httpMethod))
                {
                    continue;
                }

                var route = GetRoute(controllerRoute, method);
                var description = GetDescription(method);

                var actionInfo = new ActionInfo
                {
                    ControllerName = controllerName,
                    ActionName = method.Name,
                    HttpMethod = httpMethod,
                    Route = route,
                    Description = description
                };

                actionInfos.Add(actionInfo);
            }
        }

        return actionInfos;
    }

    private static string GetHttpMethod(MethodInfo method)
    {
        // Check for HTTP method attributes
        if (method.GetCustomAttribute<HttpGetAttribute>() != null)
        {
            return "GET";
        }

        if (method.GetCustomAttribute<HttpPostAttribute>() != null)
        {
            return "POST";
        }

        if (method.GetCustomAttribute<HttpPutAttribute>() != null)
        {
            return "PUT";
        }

        if (method.GetCustomAttribute<HttpDeleteAttribute>() != null)
        {
            return "DELETE";
        }

        if (method.GetCustomAttribute<HttpPatchAttribute>() != null)
        {
            return "PATCH";
        }

        // Check for HttpMethod attribute
        var httpMethodAttr = method.GetCustomAttribute<HttpMethodAttribute>();
        if (httpMethodAttr != null && httpMethodAttr.HttpMethods.Any())
        {
            return httpMethodAttr.HttpMethods.First();
        }

        // Default to GET if no explicit method is specified
        // (though in practice, ASP.NET Core requires explicit HTTP verbs)
        return string.Empty;
    }

    private static string? GetRoute(string controllerRoute, MethodInfo method)
    {
        // Get method-level route attribute (handle multiple attributes)
        var methodRouteAttrs = method.GetCustomAttributes<RouteAttribute>(inherit: false).ToList();
        var methodRouteAttr = methodRouteAttrs.FirstOrDefault();
        var methodRoute = methodRouteAttr?.Template;

        // Combine controller and action routes
        var routeParts = new List<string>();

        if (!string.IsNullOrEmpty(controllerRoute))
        {
            routeParts.Add(controllerRoute);
        }

        if (!string.IsNullOrEmpty(methodRoute))
        {
            routeParts.Add(methodRoute);
        }
        else
        {
            routeParts.Add(method.Name);
        }

        var fullRoute = string.Join("/", routeParts);

        // Clean up route (remove leading/trailing slashes, handle //)
        fullRoute = fullRoute.Trim('/').Replace("//", "/");

        // Add api prefix if not present
        if (!fullRoute.StartsWith("api/", StringComparison.OrdinalIgnoreCase))
        {
            fullRoute = "api/" + fullRoute;
        }

        return "/" + fullRoute;
    }

    private static string? GetDescription(MethodInfo method)
    {
        if (method != null)
        {

        }
        // Try to get XML documentation comments (if available)
        // For now, return null - can be enhanced with XML documentation parsing
        return null;
    }

    private async Task RegisterActionsAsync(List<ActionInfo> actionInfos, CancellationToken cancellationToken)
    {
        var createdBy = Guid.Empty; // System user - should be set appropriately

        foreach (var actionInfo in actionInfos)
        {
            // Check if action already exists
            var exists = await _actionRepository.ExistsAsync(
                actionInfo.ControllerName,
                actionInfo.ActionName,
                actionInfo.HttpMethod,
                cancellationToken).ConfigureAwait(false);

            if (!exists)
            {
                var actionEntity = new ActionEntity
                {
                    ControllerName = actionInfo.ControllerName,
                    ActionName = actionInfo.ActionName,
                    HttpMethod = actionInfo.HttpMethod,
                    Route = actionInfo.Route,
                    Description = actionInfo.Description,
                    CreatedBy = createdBy,
                    CreatedOn = DateTime.UtcNow,
                    IsActive = true,
                    IsDeleted = false
                };

                var insertedAction = await _actionRepository.InsertAsync(actionEntity, cancellationToken).ConfigureAwait(false);

                // Create permission for this action
                var permissionName = $"{actionInfo.ControllerName}.{actionInfo.ActionName}";
                var category = actionInfo.ControllerName;

                var permissionEntity = new PermissionEntity
                {
                    Name = permissionName,
                    ActionId = insertedAction.Id,
                    Category = category,
                    Description = actionInfo.Description,
                    CreatedBy = createdBy,
                    CreatedOn = DateTime.UtcNow,
                    IsActive = false, // Inactive by default (opt-in security)
                    IsDeleted = false
                };

                await _permissionRepository.InsertAsync(permissionEntity, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}

