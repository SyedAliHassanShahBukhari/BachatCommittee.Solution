// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BachatCommittee.Data.Migrations.Extensions;
using FluentMigrator;

namespace BachatCommittee.Data.Migrations;

[Migration(1768000000)]
public class M1768000000Pools : Migration
{
    public override void Up()
    {
        Execute.Sql("CREATE EXTENSION IF NOT EXISTS pgcrypto;");
        Execute.Sql("CREATE EXTENSION IF NOT EXISTS \"uuid-ossp\";");

        Create.Table("Pools").InSchema("public")
            .WithColumn("Id").AsCustom("uuid").NotNullable().PrimaryKey().WithDefault(SystemMethods.NewGuid)
            .WithColumn("TenantId").AsCustom("uuid").NotNullable().WithColumnDescription("Tenant owning the pool")
            .WithColumn("Name").AsString(150).NotNullable().WithColumnDescription("Pool display name")
            .WithColumn("Code").AsString(50).NotNullable().WithColumnDescription("Unique pool code per tenant")
            .WithColumn("TimeZone").AsString(80).Nullable().WithColumnDescription("IANA time zone")
            .WithDefaultColumns();

        Create.UniqueConstraint("UQ_Pools_Tenant_Code")
            .OnTable("Pools").WithSchema("public")
            .Columns("TenantId", "Code");

        Create.Index("IX_Pools_TenantId").OnTable("Pools").InSchema("public").OnColumn("TenantId");
        Create.Index("IX_Pools_Name").OnTable("Pools").InSchema("public").OnColumn("Name");
        Create.Index("IX_Pools_IsActive").OnTable("Pools").InSchema("public").OnColumn("IsActive");
        Create.Index("IX_Pools_IsDeleted").OnTable("Pools").InSchema("public").OnColumn("IsDeleted");
    }

    public override void Down()
    {
        Delete.Table("Pools").InSchema("public");
    }
}
