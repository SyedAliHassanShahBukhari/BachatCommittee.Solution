// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authorization;

namespace BachatCommittee.API.Authorization;

/// <summary>
/// Attribute to require a specific permission for an action or controller.
/// Usage: [RequirePermission("Users.Create")]
/// Note: This requires the PermissionAuthorizationHandler to be registered.
/// The handler will check permissions based on the permission name.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequirePermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "Permission";

    public string PermissionName { get; }

    public RequirePermissionAttribute(string permissionName)
    {
        PermissionName = permissionName ?? throw new ArgumentNullException(nameof(permissionName));
        // Use policy format: "Permission:PermissionName"
        Policy = $"{PolicyPrefix}:{permissionName}";
    }
}

