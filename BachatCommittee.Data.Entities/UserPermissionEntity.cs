// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Dapper.Contrib.Extensions;

namespace BachatCommittee.Data.Entities;

/// <summary>
/// Maps permissions directly to users (user-specific overrides).
/// </summary>
[Table("UserPermissions")]
public class UserPermissionEntity : BaseGuidEntity
{
    public string UserId { get; set; } = string.Empty; // References AspNetUsers.Id

    public Guid PermissionId { get; set; }

    public DateTime? ExpiresOn { get; set; } // Optional: Time-limited permissions

    public bool IsRevoked { get; set; }

    public DateTime? RevokedOn { get; set; }

    public Guid? RevokedBy { get; set; } // User who revoked the permission
}

