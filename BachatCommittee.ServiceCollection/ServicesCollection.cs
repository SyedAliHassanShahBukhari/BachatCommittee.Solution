// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using BachatCommittee.Services;
using BachatCommittee.Services.Interfaces;

namespace BachatCommittee.ServiceCollection;

public static class ServicesCollection
{
    public static void AddBachatCommitteeServicesAPI(this IServiceCollection services) => AddAPIConcreteImplementations(services);
    public static void AddBachatCommitteeServicesWebPortal(this IServiceCollection services) => AddDefaultConcreteImplementations(services);

    private static void AddDefaultConcreteImplementations(IServiceCollection services)
    {
        services.AddScoped<IUserContextService, UserContextService>();
        services.AddScoped<IExceptionLogService, ExceptionLogService>();
    }
    private static void AddAPIConcreteImplementations(IServiceCollection services)
    {
        AddDefaultConcreteImplementations(services);
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IRoleManagementService, RoleManagementService>();
        services.AddScoped<IPoolService, PoolService>();

        // Permission system services
        services.AddScoped<IActionDiscoveryService, ActionDiscoveryService>();
        services.AddScoped<IPermissionService, PermissionService>();
    }
}
