// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BachatCommittee.Models.Classes;

namespace BachatCommittee.Services.Interfaces;

public interface IUserContextService
{
    LoggedInUser LoggedInUser { get; }
    string Token { get; }
    bool IsDeveloper { get; }
    bool IsSuperAdmin { get; }
    bool IsAdmin { get; }
    bool IsCustomer { get; }
    bool IsStaff { get; }
    IReadOnlyCollection<string> Roles { get; }
    bool IsAuthenticated { get; }

    bool CanAccessDeveloperOnly();
    bool CanAccessRole(string requiredRole);
    bool CanAccessRoles(params string[] requiredRoles);
    bool HasAllRoles(params string[] roles);
    bool HasAnyRole(params string[] roles);
    bool HasRole(string role);
}
