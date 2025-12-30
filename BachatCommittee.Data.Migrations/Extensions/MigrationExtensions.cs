// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FluentMigrator;
using FluentMigrator.Builders.Create.Table;

namespace BachatCommittee.Data.Migrations.Extensions;

/// <summary>
/// Extension methods for FluentMigrator to add standard base entity columns.
/// </summary>
public static class MigrationExtensions
{
    /// <summary>
    /// Adds standard BaseEntity columns to a table creation.
    /// PostgreSQL version - uses ModifiedOn/ModifiedBy (not UpdatedOn/UpdatedBy) and UUID types.
    /// </summary>
    public static ICreateTableColumnOptionOrWithColumnSyntax WithDefaultColumns(this ICreateTableWithColumnSyntax table)
    {
        if (table == null)
        {
            throw new ArgumentNullException(nameof(table), "table param cannot be null");
        }

        return table
            .WithColumn("CreatedOn").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime).WithColumnDescription("Record creation timestamp")
            .WithColumn("CreatedBy").AsCustom("uuid").NotNullable().WithColumnDescription("User who created the record")
            .WithColumn("ModifiedOn").AsDateTime().Nullable().WithColumnDescription("Last modification timestamp")
            .WithColumn("ModifiedBy").AsCustom("uuid").Nullable().WithColumnDescription("User who last modified the record")
            .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true).WithColumnDescription("Active flag")
            .WithColumn("IsDeleted").AsBoolean().NotNullable().WithDefaultValue(false).WithColumnDescription("Soft delete flag");
    }
}

