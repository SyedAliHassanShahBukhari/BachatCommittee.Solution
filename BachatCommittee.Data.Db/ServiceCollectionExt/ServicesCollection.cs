// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using awisk.common.Data.Db.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using BachatCommittee.Data.Repos;
using BachatCommittee.Data.Repos.Interfaces;

namespace BachatCommittee.Data.Db.ServiceCollectionExt;

public static class ServicesCollection
{
    public static void AddBachatCommitteeRepos(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton<IRepositorySettings>(new RepositorySettings(connectionString));
        AddConcreteImplementations(services);
    }

    private static void AddConcreteImplementations(IServiceCollection services)
    {
        services.AddScoped<IExceptionLogRepository, ExceptionLogRepository>();
        services.AddScoped<ISequenceRepo, SequenceRepo>();
        
        // Permission system repositories
        services.AddScoped<IActionRepository, ActionRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();
        services.AddScoped<IUserPermissionRepository, UserPermissionRepository>();
        services.AddScoped<IRoleDetailRepository, RoleDetailRepository>();
    }
}
