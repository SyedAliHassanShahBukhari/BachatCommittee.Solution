// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BachatCommittee.Data.Migrations.Extensions;
using FluentMigrator;

namespace BachatCommittee.Data.Migrations;

[Migration(1766975051)]
public class M1766975051PermissionSystemTables : Migration
{
    public override void Up()
    {
        Execute.Sql("CREATE EXTENSION IF NOT EXISTS pgcrypto;");
        Execute.Sql("CREATE EXTENSION IF NOT EXISTS \"uuid-ossp\";");
        // 1. Actions Table
        Create.Table("Actions").InSchema("public")
            .WithColumn("Id").AsCustom("uuid").NotNullable().PrimaryKey().WithDefault(SystemMethods.NewGuid).WithColumnDescription("Primary Key")
            .WithColumn("ControllerName").AsString(255).NotNullable().WithColumnDescription("Controller name without 'Controller' suffix")
            .WithColumn("ActionName").AsString(255).NotNullable().WithColumnDescription("Action method name")
            .WithColumn("HttpMethod").AsString(10).NotNullable().WithColumnDescription("HTTP method (GET, POST, PUT, DELETE, PATCH)")
            .WithColumn("Route").AsCustom("text").Nullable().WithColumnDescription("Full route path")
            .WithColumn("Description").AsCustom("text").Nullable().WithColumnDescription("Action description")
            // BaseEntity properties
            .WithDefaultColumns();

        Create.UniqueConstraint("UQ_Actions_Controller_Action_Method")
            .OnTable("Actions").WithSchema("public")
            .Columns("ControllerName", "ActionName", "HttpMethod");

        Create.Index("IX_Actions_ControllerName").OnTable("Actions").InSchema("public").OnColumn("ControllerName");
        Create.Index("IX_Actions_IsActive").OnTable("Actions").InSchema("public").OnColumn("IsActive");
        Create.Index("IX_Actions_IsDeleted").OnTable("Actions").InSchema("public").OnColumn("IsDeleted");

        // 2. Permissions Table
        Create.Table("Permissions").InSchema("public")
            .WithColumn("Id").AsCustom("uuid").NotNullable().PrimaryKey().WithDefault(SystemMethods.NewGuid).WithColumnDescription("Primary Key")
            .WithColumn("Name").AsString(500).NotNullable().Unique().WithColumnDescription("Permission name (e.g., 'Users.GetAll')")
            .WithColumn("ActionId").AsCustom("uuid").NotNullable().WithColumnDescription("Reference to Actions table")
            .WithColumn("Description").AsCustom("text").Nullable().WithColumnDescription("Permission description")
            .WithColumn("Category").AsString(100).Nullable().WithColumnDescription("Permission category (e.g., 'Users', 'Roles')")
            // BaseEntity properties
            .WithDefaultColumns();

        Create.ForeignKey("FK_Permissions_ActionId")
            .FromTable("Permissions").InSchema("public").ForeignColumn("ActionId")
            .ToTable("Actions").InSchema("public").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.Index("IX_Permissions_ActionId").OnTable("Permissions").InSchema("public").OnColumn("ActionId");
        Create.Index("IX_Permissions_Category").OnTable("Permissions").InSchema("public").OnColumn("Category");
        Create.Index("IX_Permissions_IsActive").OnTable("Permissions").InSchema("public").OnColumn("IsActive");
        Create.Index("IX_Permissions_IsDeleted").OnTable("Permissions").InSchema("public").OnColumn("IsDeleted");

        // 3. RoleDetails Table (extends AspNetRoles)
        Create.Table("RoleDetails").InSchema("public")
            .WithColumn("RoleId").AsString(450).NotNullable().PrimaryKey().WithColumnDescription("Reference to AspNetRoles.Id")
            .WithColumn("Description").AsCustom("text").Nullable().WithColumnDescription("Role description")
            .WithColumn("IsPreDefined").AsBoolean().NotNullable().WithDefaultValue(false).WithColumnDescription("Pre-defined role flag")
            .WithColumn("IsSystemRole").AsBoolean().NotNullable().WithDefaultValue(false).WithColumnDescription("System role (cannot be deleted)")
            // BaseEntity properties
            .WithDefaultColumns();

        Create.ForeignKey("FK_RoleDetails_RoleId")
            .FromTable("RoleDetails").InSchema("public").ForeignColumn("RoleId")
            .ToTable("AspNetRoles").InSchema("public").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.Index("IX_RoleDetails_IsPreDefined").OnTable("RoleDetails").InSchema("public").OnColumn("IsPreDefined");
        Create.Index("IX_RoleDetails_IsSystemRole").OnTable("RoleDetails").InSchema("public").OnColumn("IsSystemRole");
        Create.Index("IX_RoleDetails_IsActive").OnTable("RoleDetails").InSchema("public").OnColumn("IsActive");
        Create.Index("IX_RoleDetails_IsDeleted").OnTable("RoleDetails").InSchema("public").OnColumn("IsDeleted");

        // 4. RolePermissions Table
        Create.Table("RolePermissions").InSchema("public")
            .WithColumn("Id").AsCustom("uuid").NotNullable().PrimaryKey().WithDefault(SystemMethods.NewGuid).WithColumnDescription("Primary Key")
            .WithColumn("RoleId").AsString(450).NotNullable().WithColumnDescription("Reference to AspNetRoles.Id")
            .WithColumn("PermissionId").AsCustom("uuid").NotNullable().WithColumnDescription("Reference to Permissions.Id")
            // BaseEntity properties
            .WithDefaultColumns();

        Create.ForeignKey("FK_RolePermissions_RoleId")
            .FromTable("RolePermissions").InSchema("public").ForeignColumn("RoleId")
            .ToTable("AspNetRoles").InSchema("public").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.ForeignKey("FK_RolePermissions_PermissionId")
            .FromTable("RolePermissions").InSchema("public").ForeignColumn("PermissionId")
            .ToTable("Permissions").InSchema("public").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.UniqueConstraint("UQ_RolePermissions_Role_Permission")
            .OnTable("RolePermissions").WithSchema("public")
            .Columns("RoleId", "PermissionId");

        Create.Index("IX_RolePermissions_RoleId").OnTable("RolePermissions").InSchema("public").OnColumn("RoleId");
        Create.Index("IX_RolePermissions_PermissionId").OnTable("RolePermissions").InSchema("public").OnColumn("PermissionId");
        Create.Index("IX_RolePermissions_IsActive").OnTable("RolePermissions").InSchema("public").OnColumn("IsActive");
        Create.Index("IX_RolePermissions_IsDeleted").OnTable("RolePermissions").InSchema("public").OnColumn("IsDeleted");

        // 5. UserPermissions Table
        Create.Table("UserPermissions").InSchema("public")
            .WithColumn("Id").AsCustom("uuid").NotNullable().PrimaryKey().WithDefault(SystemMethods.NewGuid).WithColumnDescription("Primary Key")
            .WithColumn("UserId").AsString(450).NotNullable().WithColumnDescription("Reference to AspNetUsers.Id")
            .WithColumn("PermissionId").AsCustom("uuid").NotNullable().WithColumnDescription("Reference to Permissions.Id")
            .WithColumn("ExpiresOn").AsDateTime().Nullable().WithColumnDescription("Permission expiration date (optional)")
            .WithColumn("IsRevoked").AsBoolean().NotNullable().WithDefaultValue(false).WithColumnDescription("Revocation flag")
            .WithColumn("RevokedOn").AsDateTime().Nullable().WithColumnDescription("Revocation timestamp")
            .WithColumn("RevokedBy").AsCustom("uuid").Nullable().WithColumnDescription("User who revoked the permission")
            // BaseEntity properties
            .WithDefaultColumns();

        Create.ForeignKey("FK_UserPermissions_UserId")
            .FromTable("UserPermissions").InSchema("public").ForeignColumn("UserId")
            .ToTable("AspNetUsers").InSchema("public").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.ForeignKey("FK_UserPermissions_PermissionId")
            .FromTable("UserPermissions").InSchema("public").ForeignColumn("PermissionId")
            .ToTable("Permissions").InSchema("public").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.UniqueConstraint("UQ_UserPermissions_User_Permission")
            .OnTable("UserPermissions").WithSchema("public")
            .Columns("UserId", "PermissionId");

        Create.Index("IX_UserPermissions_UserId").OnTable("UserPermissions").InSchema("public").OnColumn("UserId");
        Create.Index("IX_UserPermissions_PermissionId").OnTable("UserPermissions").InSchema("public").OnColumn("PermissionId");
        Create.Index("IX_UserPermissions_IsRevoked").OnTable("UserPermissions").InSchema("public").OnColumn("IsRevoked");
        Create.Index("IX_UserPermissions_ExpiresOn").OnTable("UserPermissions").InSchema("public").OnColumn("ExpiresOn");
        Create.Index("IX_UserPermissions_IsActive").OnTable("UserPermissions").InSchema("public").OnColumn("IsActive");
        Create.Index("IX_UserPermissions_IsDeleted").OnTable("UserPermissions").InSchema("public").OnColumn("IsDeleted");

        // 6. PermissionAuditLog Table
        Create.Table("PermissionAuditLog").InSchema("public")
            .WithColumn("Id").AsCustom("uuid").NotNullable().PrimaryKey().WithDefault(SystemMethods.NewGuid).WithColumnDescription("Primary Key")
            .WithColumn("UserId").AsString(450).Nullable().WithColumnDescription("User affected by the change")
            .WithColumn("PermissionId").AsCustom("uuid").Nullable().WithColumnDescription("Permission affected by the change")
            .WithColumn("RoleId").AsString(450).Nullable().WithColumnDescription("Role affected by the change")
            .WithColumn("Action").AsString(50).NotNullable().WithColumnDescription("Action type (GRANT, REVOKE, CREATE, DELETE, UPDATE)")
            .WithColumn("EntityType").AsString(50).NotNullable().WithColumnDescription("Entity type (USER_PERMISSION, ROLE_PERMISSION, PERMISSION, ROLE)")
            .WithColumn("Details").AsCustom("jsonb").Nullable().WithColumnDescription("Additional details as JSON")
            // BaseEntity properties
            .WithDefaultColumns();

        Create.ForeignKey("FK_PermissionAuditLog_PermissionId")
            .FromTable("PermissionAuditLog").InSchema("public").ForeignColumn("PermissionId")
            .ToTable("Permissions").InSchema("public").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.SetNull);

        Create.Index("IX_PermissionAuditLog_UserId").OnTable("PermissionAuditLog").InSchema("public").OnColumn("UserId");
        Create.Index("IX_PermissionAuditLog_PermissionId").OnTable("PermissionAuditLog").InSchema("public").OnColumn("PermissionId");
        Create.Index("IX_PermissionAuditLog_RoleId").OnTable("PermissionAuditLog").InSchema("public").OnColumn("RoleId");
        Create.Index("IX_PermissionAuditLog_Action").OnTable("PermissionAuditLog").InSchema("public").OnColumn("Action");
        Create.Index("IX_PermissionAuditLog_EntityType").OnTable("PermissionAuditLog").InSchema("public").OnColumn("EntityType");
    }

    public override void Down()
    {
        Delete.Table("PermissionAuditLog").InSchema("public");
        Delete.Table("UserPermissions").InSchema("public");
        Delete.Table("RolePermissions").InSchema("public");
        Delete.Table("RoleDetails").InSchema("public");
        Delete.Table("Permissions").InSchema("public");
        Delete.Table("Actions").InSchema("public");
    }
}

