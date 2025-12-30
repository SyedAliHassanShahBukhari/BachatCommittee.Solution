// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Dapper.Contrib.Extensions;

namespace BachatCommittee.Data.Entities;

/// <summary>
/// Maps permissions to roles.
/// </summary>
[Table("RolePermissions")]
public class RolePermissionEntity : BaseGuidEntity
{
    public string RoleId { get; set; } = string.Empty; // References AspNetRoles.Id

    public Guid PermissionId { get; set; }
}

