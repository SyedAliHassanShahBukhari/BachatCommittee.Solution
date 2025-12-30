// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Dapper.Contrib.Extensions;

namespace BachatCommittee.Data.Entities;

/// <summary>
/// Represents a savings committee pool scoped to a tenant.
/// </summary>
[Table("Pools")]
public class PoolEntity : BaseGuidEntity
{
    /// <summary>
    /// Tenant that owns the pool.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Display name for the pool (e.g., "January 2025 Committee").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Unique code per tenant used for quick lookups and invites.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Optional IANA timezone identifier for scheduling.
    /// </summary>
    public string? TimeZone { get; set; }
}
