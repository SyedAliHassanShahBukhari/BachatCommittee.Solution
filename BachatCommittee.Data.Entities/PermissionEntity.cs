// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Dapper.Contrib.Extensions;

namespace BachatCommittee.Data.Entities;

/// <summary>
/// Represents a permission that maps to an action.
/// </summary>
[Table("Permissions")]
public class PermissionEntity : BaseGuidEntity
{
    public string Name { get; set; } = string.Empty; // e.g., "Users.GetAll", "Users.Create"

    public Guid ActionId { get; set; }

    public string? Description { get; set; }

    public string? Category { get; set; } // e.g., "Users", "Roles", "System"
}

