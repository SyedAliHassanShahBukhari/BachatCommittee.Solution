// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data;
using System.Security.Claims;
using awisk.common.Helpers;
using BachatCommittee.Models.Classes;
using BachatCommittee.Models.Enums;
using BachatCommittee.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace BachatCommittee.Services;

public class UserContextService : IUserContextService
{
    private static readonly StringComparer _roleComparer = StringComparer.OrdinalIgnoreCase;

    // Central place to define “who can access what”.
    // Key = required role; Value = roles that are allowed to access that “area”.
    private static readonly Dictionary<string, string[]> _roleAccessMap =
        new(_roleComparer)
        {
            // SuperAdmin area: SuperAdmin + Developer
            ["SuperAdmin"] = ["SuperAdmin", "Developer"],

            // Admin area: Admin + SuperAdmin + Developer
            ["Admin"] = ["Admin", "SuperAdmin", "Developer"],

            // Customer area: Customer + Developer
            ["Customer"] = ["Customer", "Developer"],

            // Staff area: Staff + Admin + SuperAdmin + Developer
            ["Staff"] = ["Staff", "Admin", "SuperAdmin", "Developer"],

            // Developer-only area
            ["Developer"] = ["Developer"]
        };
    public LoggedInUser LoggedInUser { get; private set; } = new();
    public string Token { get; private set; } = string.Empty;
    public bool IsDeveloper => HasRole("Developer");
    public bool IsSuperAdmin => HasRole("SuperAdmin");
    public bool IsAdmin => HasRole("Admin");
    public bool IsCustomer => HasRole("Customer");
    public bool IsStaff => HasRole("Staff");
    public IReadOnlyCollection<string> Roles { get; private set; } = [];
    public bool IsAuthenticated => LoggedInUser.Id != Guid.Empty;
    public bool CanAccessDeveloperOnly() => IsDeveloper;

    public UserContextService(IHttpContextAccessor accessor)
    {
        var user = accessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            LoggedInUser = new()
            {
                Id = UniversalOpertaions.TryGuid(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : Guid.Empty,
                UserName = user.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty,
                FullName = user.FindFirst("FullName")?.Value ?? string.Empty,
                Type = UniversalOpertaions.ToEnum(user.FindFirst("CustomerType")?.Value, CustomerType.CA),
                UserType = UniversalOpertaions.ToEnum(user.FindFirst("UserType")?.Value, UserType.User),
            };

            Token = user.FindFirst("Token")?.Value ?? string.Empty;

            // Load all roles dynamically (supports DB roles automatically)
            Roles = [.. user
                .Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .Distinct(_roleComparer)];
        }
    }

    public bool HasRole(string role)
    {
        return !string.IsNullOrWhiteSpace(role) && Roles.Count != 0 && Roles.Contains(role, _roleComparer);
    }

    public bool HasAnyRole(params string[] roles)
    {
        if (roles is null || roles.Length == 0 || Roles.Count == 0)
        {
            return false;
        }

        foreach (var role in roles)
        {
            if (!string.IsNullOrWhiteSpace(role) && HasRole(role))
            {
                return true;
            }
        }

        return false;
    }

    public bool HasAllRoles(params string[] roles)
    {
        if (roles is null || roles.Length == 0)
        {
            return false;
        }

        foreach (var role in roles)
        {
            if (string.IsNullOrWhiteSpace(role) || !HasRole(role))
            {
                return false;
            }
        }

        return true;
    }

    public bool CanAccessRole(string requiredRole)
    {
        if (string.IsNullOrWhiteSpace(requiredRole))
        {
            return false;
        }

        // If we have a rule in the map, use that
        if (_roleAccessMap.TryGetValue(requiredRole, out var allowedRoles))
        {
            return HasAnyRole(allowedRoles);
        }

        // Fallback for NEW roles that are only in DB:
        // By default: same role OR Developer can access that area.
        return HasAnyRole(requiredRole, "Developer");
    }

    public bool CanAccessRoles(params string[] requiredRoles)
    {
        if (requiredRoles is null || requiredRoles.Length == 0)
        {
            return false;
        }

        // If user can access ANY of the required roles, we return true.
        foreach (var requiredRole in requiredRoles)
        {
            if (CanAccessRole(requiredRole))
            {
                return true;
            }
        }

        return false;
    }
}
