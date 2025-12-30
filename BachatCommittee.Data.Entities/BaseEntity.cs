// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Dapper.Contrib.Extensions;

namespace BachatCommittee.Data.Entities;

/// <summary>
/// Base entity class with standard audit and soft delete properties.
/// Matches the columns defined in MigrationExtensions.WithDefaultColumns().
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Record creation timestamp. Set automatically on creation.
    /// </summary>
    public DateTime CreatedOn { get; set; }

    /// <summary>
    /// User who created the record (UUID).
    /// </summary>
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// Last modification timestamp. Nullable, set on updates.
    /// </summary>
    public DateTime? ModifiedOn { get; set; }

    /// <summary>
    /// User who last modified the record (UUID). Nullable.
    /// </summary>
    public Guid? ModifiedBy { get; set; }

    /// <summary>
    /// Active flag. Defaults to true.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Soft delete flag. Defaults to false.
    /// </summary>
    public bool IsDeleted { get; set; }
}

/// <summary>
/// Base entity class with GUID primary key and standard audit properties.
/// </summary>
public abstract class BaseGuidEntity : BaseEntity
{
    /// <summary>
    /// Primary key (UUID).
    /// </summary>
    [ExplicitKey]
    public Guid Id { get; set; } = Guid.NewGuid();
}

/// <summary>
/// Base entity class with generic primary key and standard audit properties.
/// </summary>
public abstract class BaseEntity<TKey> : BaseEntity where TKey : notnull
{
    /// <summary>
    /// Primary key of generic type.
    /// </summary>
    public TKey Id { get; set; } = default!;
}

