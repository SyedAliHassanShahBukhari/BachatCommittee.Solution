// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using awisk.common.Extensions;
using FluentMigrator;

namespace BachatCommittee.Data.Migrations;

[Migration(1763357660)]
public class M1763357660ExceptionLogTable : Migration
{
    public override void Up()
    {
        Create.Table("ExceptionLogs")
            .WithColumn("LogId").AsInt64().NotNullable().PrimaryKey().Identity().WithColumnDescription("Primary Key of the table")
            .WithColumn("Message").AsCustom("text").NotNullable()
            .WithColumn("StackTrace").AsCustom("text").NotNullable()
            .WithColumn("Type").AsString(500).NotNullable()
            .WithColumn("URL").AsCustom("text").NotNullable()
            .WithColumn("CreatedOn").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);
    }
    public override void Down()
    {
        Delete.Table("ExceptionLogs");
    }
}
