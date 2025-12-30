// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Dapper.Contrib.Extensions;

namespace BachatCommittee.Data.Entities;

/// <summary>
/// Extends AspNetRoles with additional metadata for the permission system.
/// Uses RoleId as the primary key (references AspNetRoles.Id).
/// </summary>
[Table("RoleDetails")]
public class RoleDetailEntity : BaseEntity<string>
{
    [Key]
    public string RoleId { get; set; } = string.Empty; // References AspNetRoles.Id

    public string? Description { get; set; }

    public bool IsPreDefined { get; set; }

    public bool IsSystemRole { get; set; } // Cannot be deleted
}

