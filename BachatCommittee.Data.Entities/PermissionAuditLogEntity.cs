// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Dapper.Contrib.Extensions;

namespace BachatCommittee.Data.Entities;

/// <summary>
/// Audit log for tracking permission changes.
/// Uses JSONB for flexible detail storage (PostgreSQL-specific).
/// </summary>
[Table("PermissionAuditLog")]
public class PermissionAuditLogEntity : BaseGuidEntity
{
    public string? UserId { get; set; } // User affected by the change (nullable)

    public Guid? PermissionId { get; set; } // Permission affected (nullable)

    public string? RoleId { get; set; } // Role affected (nullable)

    public string Action { get; set; } = string.Empty; // GRANT, REVOKE, CREATE, DELETE, UPDATE

    public string EntityType { get; set; } = string.Empty; // USER_PERMISSION, ROLE_PERMISSION, PERMISSION, ROLE

    /// <summary>
    /// Additional details as JSON (stored as JSONB in PostgreSQL).
    /// Can be deserialized to a dictionary or custom object.
    /// </summary>
    public string? Details { get; set; } // JSON string - use JsonSerializer to serialize/deserialize
}

