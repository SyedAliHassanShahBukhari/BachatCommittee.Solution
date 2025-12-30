// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BachatCommittee.Models.DTOs.Responses;

/// <summary>
/// Response DTO containing user's effective permissions (role + user-specific).
/// </summary>
public class UserEffectivePermissionsDto
{
    public string UserId { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public List<PermissionDto> RolePermissions { get; set; } = new();
    public List<PermissionDto> UserPermissions { get; set; } = new();
    public List<PermissionDto> AllPermissions { get; set; } = new(); // Combined and deduplicated
}

