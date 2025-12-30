# Dynamic Permission System Architecture Design

## Overview

This document describes the architecture for a dynamic, database-driven permission system that automatically discovers all controllers and actions in the application and allows administrators to manage roles and user-specific permissions.

## Table of Contents

1. [System Requirements](#system-requirements)
2. [Database Schema Design](#database-schema-design)
3. [Architecture Components](#architecture-components)
4. [Implementation Details](#implementation-details)
5. [API Endpoints](#api-endpoints)
6. [Authorization Flow](#authorization-flow)
7. [Migration Strategy](#migration-strategy)
8. [Security Considerations](#security-considerations)

---

## System Requirements

### Functional Requirements

1. **Automatic Action Discovery**
   - System must scan all controllers and actions at startup
   - Register discovered actions in the database
   - Support HTTP methods (GET, POST, PUT, DELETE, PATCH, etc.)
   - Track controller and action metadata

2. **Role Management**
   - Create pre-defined roles (Staff, Admin, Driver, SuperAdmin, etc.)
   - Assign permissions to roles
   - Multiple permissions per role
   - Role hierarchy support (optional)

3. **User-Specific Permissions**
   - Assign permissions directly to users
   - Override role-based permissions
   - Users inherit role permissions + user-specific permissions
   - Permission precedence: User > Role

4. **Authorization**
   - Check permissions at controller/action level
   - Support both role-based and permission-based authorization
   - Backward compatible with existing `[Authorize(Roles = "...")]` attributes

5. **Administration**
   - API endpoints for managing permissions
   - Assign/revoke permissions to/from roles
   - Assign/revoke permissions to/from users
   - View user's effective permissions

### Non-Functional Requirements

- Performance: Permission checks should be fast (< 10ms)
- Scalability: Support thousands of users and hundreds of permissions
- Security: Permission data must be protected
- Auditability: Track permission changes

---

## Database Schema Design

### Entity Relationship Diagram

```
┌─────────────────┐
│     Actions     │
│─────────────────│
│ Id (PK - UUID)  │
│ ControllerName  │
│ ActionName      │
│ HttpMethod      │
│ Route (TEXT)    │
│ Description     │
│ +BaseEntity     │
└────────┬────────┘
         │ 1
         │
         │ N
┌────────▼────────┐
│   Permissions   │
│─────────────────│
│ Id (PK - UUID)  │
│ Name (Unique)   │
│ ActionId (FK)   │
│ Category        │
│ Description     │
│ +BaseEntity     │
└────┬──────┬─────┘
     │      │
     │ N    │ N
     │      │
┌────▼──────┴─────┐       ┌──────────────────┐
│ RolePermissions │       │ AspNetRoles      │
│─────────────────│       │──────────────────│
│ Id (PK - UUID)  │       │ Id (PK)          │
│ RoleId (FK)─────┼───────┤ Name             │
│ PermissionId(FK)│       │ NormalizedName   │
│ +BaseEntity     │       └──────────────────┘
└─────────────────┘

┌─────────────────┐       ┌──────────────────┐
│ UserPermissions │       │ AspNetUsers      │
│─────────────────│       │──────────────────│
│ Id (PK - UUID)  │       │ Id (PK)          │
│ UserId (FK)─────┼───────┤ Email            │
│ PermissionId(FK)│       │ UserName         │
│ ExpiresOn       │       │ ...              │
│ IsRevoked       │       └──────────────────┘
│ +BaseEntity     │
└─────────────────┘

BaseEntity includes:
- CreatedBy (UUID)
- CreatedOn (TIMESTAMP)
- ModifiedBy (UUID, nullable)
- ModifiedOn (TIMESTAMP, nullable)
- IsDeleted (BOOLEAN)
- IsActive (BOOLEAN)
```

### BaseEntity Structure

All tables will follow a standard BaseEntity structure for consistency and auditability. This pattern provides:

- **Audit Trail**: Track who created/modified records and when
- **Soft Delete**: Mark records as deleted without actually removing them
- **Active/Inactive**: Control record visibility without deletion
- **Security**: UUID primary keys prevent ID enumeration attacks

**Standard BaseEntity Properties:**
- `"Id"` UUID PRIMARY KEY DEFAULT gen_random_uuid() (non-predictable, secure identifiers)
- `"CreatedBy"` UUID NOT NULL (user who created the record)
- `"CreatedOn"` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
- `"ModifiedBy"` UUID (user who last modified the record, nullable)
- `"ModifiedOn"` TIMESTAMP (last modification time, nullable)
- `"IsDeleted"` BOOLEAN NOT NULL DEFAULT false (soft delete flag)
- `"IsActive"` BOOLEAN NOT NULL DEFAULT true (active/inactive flag)

**Note:** The `RoleDetails` table uses `RoleId` as its primary key (since it extends Identity's `AspNetRoles`), but still includes all other BaseEntity properties.

### 1. Actions Table

Stores all discovered controller actions.

```sql
CREATE TABLE "Actions" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "ControllerName" VARCHAR(255) NOT NULL,
    "ActionName" VARCHAR(255) NOT NULL,
    "HttpMethod" VARCHAR(10) NOT NULL, -- GET, POST, PUT, DELETE, PATCH
    "Route" TEXT, -- Full route path (TEXT for unlimited length)
    "Description" TEXT,
    -- BaseEntity properties
    "CreatedBy" UUID NOT NULL,
    "CreatedOn" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "ModifiedBy" UUID,
    "ModifiedOn" TIMESTAMP,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "IsActive" BOOLEAN NOT NULL DEFAULT true,
    CONSTRAINT "UQ_Actions_Controller_Action_Method" UNIQUE ("ControllerName", "ActionName", "HttpMethod")
);

CREATE INDEX "IX_Actions_ControllerName" ON "Actions" ("ControllerName");
CREATE INDEX "IX_Actions_IsActive" ON "Actions" ("IsActive");
CREATE INDEX "IX_Actions_IsDeleted" ON "Actions" ("IsDeleted");
```

**Example Data:**
```
Id                                   | ControllerName | ActionName     | HttpMethod | Route
-------------------------------------|----------------|----------------|------------|------------------
a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d| UsersController| GetAllUsers    | GET        | api/v1/Users
b2c3d4e5-f6a7-4b8c-9d0e-1f2a3b4c5d6e| UsersController| GetUserById    | GET        | api/v1/Users/{id}
c3d4e5f6-a7b8-4c9d-0e1f-2a3b4c5d6e7f| UsersController| CreateUser     | POST       | api/v1/Users
d4e5f6a7-b8c9-4d0e-1f2a-3b4c5d6e7f8a| RolesController| GetAllRoles    | GET        | api/v1/Roles
```

### 2. Permissions Table

Maps to Actions - represents the permission to execute an action.

```sql
CREATE TABLE "Permissions" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "Name" VARCHAR(255) NOT NULL UNIQUE, -- e.g., "Users.GetAll", "Users.Create"
    "ActionId" UUID NOT NULL,
    "Description" TEXT,
    "Category" VARCHAR(100), -- e.g., "Users", "Roles", "System"
    -- BaseEntity properties
    "CreatedBy" UUID NOT NULL,
    "CreatedOn" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "ModifiedBy" UUID,
    "ModifiedOn" TIMESTAMP,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "IsActive" BOOLEAN NOT NULL DEFAULT true,
    CONSTRAINT "FK_Permissions_ActionId" FOREIGN KEY ("ActionId") REFERENCES "Actions"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_Permissions_ActionId" ON "Permissions" ("ActionId");
CREATE INDEX "IX_Permissions_Category" ON "Permissions" ("Category");
CREATE INDEX "IX_Permissions_IsActive" ON "Permissions" ("IsActive");
CREATE INDEX "IX_Permissions_IsDeleted" ON "Permissions" ("IsDeleted");
CREATE INDEX "IX_Permissions_Name" ON "Permissions" ("Name"); -- For permission name lookups
```

**PostgreSQL-Specific Notes:**
- Use `UUID` with `gen_random_uuid()` for primary keys (secure, non-predictable identifiers)
- Use `TIMESTAMP` for UTC datetime (use `TIMESTAMPTZ` if timezone-aware)
- Use `TEXT` for potentially long strings (Description fields, Routes)
- Use `VARCHAR(n)` for fixed-length strings (Names, IDs) where appropriate
- All identifiers are case-sensitive in PostgreSQL, use quoted identifiers for consistency
- All tables follow BaseEntity structure for consistency and auditability

**Permission Naming Convention:**
- Format: `{Controller}.{Action}` or `{Category}.{Action}`
- Examples:
  - `Users.GetAll` - Get all users
  - `Users.Create` - Create a user
  - `Users.Update` - Update a user
  - `Users.Delete` - Delete a user
  - `Roles.Manage` - Manage roles
  - `Permissions.View` - View permissions
  - `Permissions.Manage` - Manage permissions

**Example Data:**
```
Id                                   | Name              | ActionId                            | Category | Description
-------------------------------------|-------------------|-------------------------------------|----------|------------------
e5f6a7b8-c9d0-4e1f-2a3b-4c5d6e7f8a9b| Users.GetAll      | a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d| Users    | View all users
f6a7b8c9-d0e1-4f2a-3b4c-5d6e7f8a9b0c| Users.Create      | c3d4e5f6-a7b8-4c9d-0e1f-2a3b4c5d6e7f| Users    | Create new users
a7b8c9d0-e1f2-4a3b-4c5d-6e7f8a9b0c1d| Users.Update      | [UUID]                              | Users    | Update users
b8c9d0e1-f2a3-4b4c-5d6e-7f8a9b0c1d2e| Roles.Manage      | [UUID]                              | Roles    | Manage roles
```

### 3. Roles Table (Enhanced)

Extend existing AspNetRoles or create a new table.

**Option A: Extend AspNetRoles (Recommended)**

This approach extends the existing Identity `AspNetRoles` table with additional metadata without modifying the Identity schema.

```sql
CREATE TABLE "RoleDetails" (
    "RoleId" VARCHAR(450) PRIMARY KEY, -- References AspNetRoles.Id (Identity uses VARCHAR)
    "Description" TEXT,
    "IsPreDefined" BOOLEAN NOT NULL DEFAULT false,
    "IsSystemRole" BOOLEAN NOT NULL DEFAULT false, -- Cannot be deleted
    -- BaseEntity properties (Note: RoleId is the PK, not a separate Id)
    "CreatedBy" UUID NOT NULL,
    "CreatedOn" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "ModifiedBy" UUID,
    "ModifiedOn" TIMESTAMP,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "IsActive" BOOLEAN NOT NULL DEFAULT true,
    CONSTRAINT "FK_RoleDetails_RoleId" FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_RoleDetails_IsPreDefined" ON "RoleDetails" ("IsPreDefined");
CREATE INDEX "IX_RoleDetails_IsSystemRole" ON "RoleDetails" ("IsSystemRole");
CREATE INDEX "IX_RoleDetails_IsActive" ON "RoleDetails" ("IsActive");
CREATE INDEX "IX_RoleDetails_IsDeleted" ON "RoleDetails" ("IsDeleted");
```

**Note:** Since `RoleDetails` extends Identity's `AspNetRoles` table, it uses `RoleId` as the primary key (which references `AspNetRoles.Id`). The BaseEntity structure is still followed with `CreatedBy`, `CreatedOn`, etc., but there's no separate `Id` column since `RoleId` serves that purpose.

**Benefits:**
- Non-intrusive (doesn't modify Identity tables)
- Leverages existing Identity role management
- Easy to query role metadata
- Supports existing role assignment patterns

**Option B: Separate CustomRoles Table**
```sql
CREATE TABLE "CustomRoles" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "Name" VARCHAR(500) NOT NULL UNIQUE, -- Increased to 500 for flexibility
    "NormalizedName" VARCHAR(500) NOT NULL UNIQUE, -- Increased to 500 for flexibility
    "Description" TEXT,
    "IsPreDefined" BOOLEAN NOT NULL DEFAULT false,
    "IsSystemRole" BOOLEAN NOT NULL DEFAULT false,
    -- BaseEntity properties
    "CreatedBy" UUID NOT NULL,
    "CreatedOn" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "ModifiedBy" UUID,
    "ModifiedOn" TIMESTAMP,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "IsActive" BOOLEAN NOT NULL DEFAULT true
);

CREATE INDEX "IX_CustomRoles_NormalizedName" ON "CustomRoles" ("NormalizedName");
CREATE INDEX "IX_CustomRoles_IsActive" ON "CustomRoles" ("IsActive");
CREATE INDEX "IX_CustomRoles_IsDeleted" ON "CustomRoles" ("IsDeleted");
```

**Note on VARCHAR(500) for Name/NormalizedName:** Using VARCHAR(500) provides flexibility for longer role names while still being indexable and queryable efficiently. This is a reasonable balance between flexibility and performance. PostgreSQL handles VARCHAR efficiently, and 500 characters is sufficient for most role naming scenarios while maintaining good index performance.

### 4. RolePermissions Table

Maps permissions to roles.

```sql
CREATE TABLE "RolePermissions" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "RoleId" VARCHAR(450) NOT NULL, -- References AspNetRoles.Id (Identity uses VARCHAR)
    "PermissionId" UUID NOT NULL,
    -- BaseEntity properties
    "CreatedBy" UUID NOT NULL, -- User who granted the permission
    "CreatedOn" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "ModifiedBy" UUID,
    "ModifiedOn" TIMESTAMP,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "IsActive" BOOLEAN NOT NULL DEFAULT true,
    CONSTRAINT "FK_RolePermissions_RoleId" FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_RolePermissions_PermissionId" FOREIGN KEY ("PermissionId") REFERENCES "Permissions"("Id") ON DELETE CASCADE,
    CONSTRAINT "UQ_RolePermissions_Role_Permission" UNIQUE ("RoleId", "PermissionId")
);

CREATE INDEX "IX_RolePermissions_RoleId" ON "RolePermissions" ("RoleId");
CREATE INDEX "IX_RolePermissions_PermissionId" ON "RolePermissions" ("PermissionId");
CREATE INDEX "IX_RolePermissions_IsActive" ON "RolePermissions" ("IsActive");
CREATE INDEX "IX_RolePermissions_IsDeleted" ON "RolePermissions" ("IsDeleted");
```

### 5. UserPermissions Table

Maps permissions directly to users (user-specific overrides).

```sql
CREATE TABLE "UserPermissions" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId" VARCHAR(450) NOT NULL, -- References AspNetUsers.Id (Identity uses VARCHAR)
    "PermissionId" UUID NOT NULL,
    "ExpiresOn" TIMESTAMP, -- Optional: Time-limited permissions
    "IsRevoked" BOOLEAN NOT NULL DEFAULT false,
    "RevokedOn" TIMESTAMP,
    "RevokedBy" UUID, -- User who revoked the permission
    -- BaseEntity properties
    "CreatedBy" UUID NOT NULL, -- User who granted the permission
    "CreatedOn" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "ModifiedBy" UUID,
    "ModifiedOn" TIMESTAMP,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "IsActive" BOOLEAN NOT NULL DEFAULT true,
    CONSTRAINT "FK_UserPermissions_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_UserPermissions_PermissionId" FOREIGN KEY ("PermissionId") REFERENCES "Permissions"("Id") ON DELETE CASCADE,
    CONSTRAINT "UQ_UserPermissions_User_Permission" UNIQUE ("UserId", "PermissionId")
);

CREATE INDEX "IX_UserPermissions_UserId" ON "UserPermissions" ("UserId");
CREATE INDEX "IX_UserPermissions_PermissionId" ON "UserPermissions" ("PermissionId");
CREATE INDEX "IX_UserPermissions_IsRevoked" ON "UserPermissions" ("IsRevoked");
CREATE INDEX "IX_UserPermissions_ExpiresOn" ON "UserPermissions" ("ExpiresOn");
CREATE INDEX "IX_UserPermissions_IsActive" ON "UserPermissions" ("IsActive");
CREATE INDEX "IX_UserPermissions_IsDeleted" ON "UserPermissions" ("IsDeleted");
```

### 6. PermissionAuditLog Table (Optional but Recommended)

Track changes to permissions for audit purposes. Uses JSONB for flexible detail storage (PostgreSQL-specific feature).

**PostgreSQL JSONB Benefits:**
- Efficient storage and querying of JSON data
- Can index JSONB columns for fast queries
- Flexible schema for different audit log entry types

```sql
CREATE TABLE "PermissionAuditLog" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId" VARCHAR(450), -- User affected by the change (nullable)
    "PermissionId" UUID, -- Permission affected (nullable)
    "RoleId" VARCHAR(450), -- Role affected (nullable)
    "Action" VARCHAR(50) NOT NULL, -- GRANT, REVOKE, CREATE, DELETE, UPDATE
    "EntityType" VARCHAR(50) NOT NULL, -- USER_PERMISSION, ROLE_PERMISSION, PERMISSION, ROLE
    "Details" JSONB, -- Additional details as JSON
    -- BaseEntity properties
    "CreatedBy" UUID NOT NULL, -- User who made the change (ChangedBy equivalent)
    "CreatedOn" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "ModifiedBy" UUID, -- Usually NULL for audit logs (immutable)
    "ModifiedOn" TIMESTAMP, -- Usually NULL for audit logs (immutable)
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false, -- Soft delete for audit logs (rarely used)
    "IsActive" BOOLEAN NOT NULL DEFAULT true,
    CONSTRAINT "FK_PermissionAuditLog_CreatedBy" FOREIGN KEY ("CreatedBy") REFERENCES "AspNetUsers"("Id"),
    CONSTRAINT "FK_PermissionAuditLog_PermissionId" FOREIGN KEY ("PermissionId") REFERENCES "Permissions"("Id") ON DELETE SET NULL
);

CREATE INDEX "IX_PermissionAuditLog_UserId" ON "PermissionAuditLog" ("UserId");
CREATE INDEX "IX_PermissionAuditLog_PermissionId" ON "PermissionAuditLog" ("PermissionId");
CREATE INDEX "IX_PermissionAuditLog_RoleId" ON "PermissionAuditLog" ("RoleId");
CREATE INDEX "IX_PermissionAuditLog_CreatedOn" ON "PermissionAuditLog" ("CreatedOn");
CREATE INDEX "IX_PermissionAuditLog_Action" ON "PermissionAuditLog" ("Action");
CREATE INDEX "IX_PermissionAuditLog_EntityType" ON "PermissionAuditLog" ("EntityType");
```

---

## Architecture Components

### 1. Action Discovery Service

**Purpose:** Automatically discover and register all controllers and actions.

**Location:** `BachatCommittee.Services/ActionDiscoveryService.cs`

**Responsibilities:**
- Scan all controllers using reflection
- Extract action methods and HTTP verbs
- Build action metadata
- Register/update actions in database
- Handle controller/action changes (additions, removals)

**Implementation:**
```csharp
public interface IActionDiscoveryService
{
    Task DiscoverAndRegisterActionsAsync(CancellationToken cancellationToken = default);
    Task<List<ActionInfo>> GetDiscoveredActionsAsync(CancellationToken cancellationToken = default);
    Task SyncActionsAsync(CancellationToken cancellationToken = default);
}

public class ActionInfo
{
    public string ControllerName { get; set; }
    public string ActionName { get; set; }
    public string HttpMethod { get; set; }
    public string Route { get; set; }
    public string? Description { get; set; }
}
```

### 2. Permission Service

**Purpose:** Manage permissions, roles, and assignments.

**Location:** `BachatCommittee.Services/PermissionService.cs`

**Responsibilities:**
- CRUD operations for permissions
- Assign/revoke permissions to/from roles
- Assign/revoke permissions to/from users
- Get user's effective permissions (role + user-specific)
- Get role permissions
- Permission caching

**Implementation:**
```csharp
public interface IPermissionService
{
    // Permission Management
    Task<List<PermissionDto>> GetAllPermissionsAsync(CancellationToken cancellationToken = default);
    Task<PermissionDto?> GetPermissionByIdAsync(Guid permissionId, CancellationToken cancellationToken = default);
    Task<List<PermissionDto>> GetPermissionsByCategoryAsync(string category, CancellationToken cancellationToken = default);

    // Role Permission Management
    Task<bool> AssignPermissionToRoleAsync(string roleId, Guid permissionId, Guid grantedBy, CancellationToken cancellationToken = default);
    Task<bool> RevokePermissionFromRoleAsync(string roleId, Guid permissionId, Guid revokedBy, CancellationToken cancellationToken = default);
    Task<List<PermissionDto>> GetRolePermissionsAsync(string roleId, CancellationToken cancellationToken = default);
    Task<bool> AssignMultiplePermissionsToRoleAsync(string roleId, List<Guid> permissionIds, Guid grantedBy, CancellationToken cancellationToken = default);

    // User Permission Management
    Task<bool> AssignPermissionToUserAsync(string userId, Guid permissionId, Guid grantedBy, DateTime? expiresOn = null, CancellationToken cancellationToken = default);
    Task<bool> RevokePermissionFromUserAsync(string userId, Guid permissionId, Guid revokedBy, CancellationToken cancellationToken = default);
    Task<List<PermissionDto>> GetUserPermissionsAsync(string userId, CancellationToken cancellationToken = default);
    Task<List<PermissionDto>> GetUserEffectivePermissionsAsync(string userId, CancellationToken cancellationToken = default);

    // Permission Checking
    Task<bool> HasPermissionAsync(string userId, string permissionName, CancellationToken cancellationToken = default);
    Task<bool> HasPermissionAsync(string userId, string controllerName, string actionName, string httpMethod, CancellationToken cancellationToken = default);
}
```

### 3. Permission Authorization Handler

**Purpose:** Authorization handler for permission-based authorization.

**Location:** `BachatCommittee.API/Authorization/PermissionAuthorizationHandler.cs`

**Implementation:**
```csharp
public class PermissionAuthorizationRequirement : IAuthorizationRequirement
{
    public string PermissionName { get; }
    public PermissionAuthorizationRequirement(string permissionName)
    {
        PermissionName = permissionName;
    }
}

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionAuthorizationRequirement>
{
    private readonly IPermissionService _permissionService;

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionAuthorizationRequirement requirement)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            context.Fail();
            return;
        }

        var hasPermission = await _permissionService.HasPermissionAsync(
            userId,
            requirement.PermissionName);

        if (hasPermission)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }
}
```

### 4. Permission Attribute

**Purpose:** Attribute to mark actions with permission requirements.

**Location:** `BachatCommittee.API/Attributes/RequirePermissionAttribute.cs`

**Implementation:**
```csharp
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class RequirePermissionAttribute : AuthorizeAttribute
{
    public string PermissionName { get; }

    public RequirePermissionAttribute(string permissionName)
    {
        PermissionName = permissionName;
        Policy = $"Permission:{permissionName}";
    }
}

// Usage:
[RequirePermission("Users.GetAll")]
public async Task<IActionResult> GetAllUsers() { ... }

[RequirePermission("Users.Create")]
public async Task<IActionResult> CreateUser() { ... }
```

### 5. Permission Middleware (Optional)

**Purpose:** Automatic permission checking based on controller/action.

**Location:** `BachatCommittee.API/Middleware/PermissionMiddleware.cs`

**Implementation:**
- Intercepts requests
- Extracts controller/action information
- Checks permissions automatically
- Can be used as a fallback for actions without explicit `[RequirePermission]` attributes

---

## Implementation Details

### Project Structure

```
BachatCommittee.Data.Entities/
  ├── ActionEntity.cs
  ├── PermissionEntity.cs
  ├── RoleDetailEntity.cs (if using separate table)
  ├── RolePermissionEntity.cs
  ├── UserPermissionEntity.cs
  └── PermissionAuditLogEntity.cs

BachatCommittee.Data.Repos/
  ├── Interfaces/
  │   ├── IActionRepository.cs
  │   ├── IPermissionRepository.cs
  │   └── IUserPermissionRepository.cs
  ├── ActionRepository.cs
  ├── PermissionRepository.cs
  └── UserPermissionRepository.cs

BachatCommittee.Services/
  ├── ActionDiscoveryService.cs
  ├── PermissionService.cs
  └── PermissionCacheService.cs (optional for performance)

BachatCommittee.Services.Interfaces/
  ├── IActionDiscoveryService.cs
  └── IPermissionService.cs

BachatCommittee.API/
  ├── Authorization/
  │   └── PermissionAuthorizationHandler.cs
  ├── Attributes/
  │   └── RequirePermissionAttribute.cs
  ├── Middleware/
  │   └── PermissionMiddleware.cs (optional)
  └── Controllers/
      └── PermissionsController.cs (Admin API)
      └── RolesPermissionsController.cs (Admin API)
```

### Action Discovery Process

**Startup Sequence:**

1. Application starts
2. `ActionDiscoveryService` scans all controllers
3. Extracts action methods using reflection:
   - Controller name
   - Action method name
   - HTTP verbs (from attributes: `[HttpGet]`, `[HttpPost]`, etc.)
   - Route information
   - Description from XML comments or attributes
4. Compares with existing actions in database
5. Inserts new actions
6. Marks removed actions as inactive (don't delete for audit)
7. Creates corresponding permissions

**Reflection Code Example:**
```csharp
public async Task DiscoverAndRegisterActionsAsync(CancellationToken cancellationToken = default)
{
    var actionInfos = new List<ActionInfo>();
    var controllers = Assembly.GetExecutingAssembly()
        .GetTypes()
        .Where(t => t.IsSubclassOf(typeof(ControllerBase)) &&
                   !t.IsAbstract)
        .ToList();

    foreach (var controller in controllers)
    {
        var controllerName = controller.Name.Replace("Controller", "");
        var methods = controller.GetMethods(
            BindingFlags.Public |
            BindingFlags.Instance |
            BindingFlags.DeclaredOnly);

        foreach (var method in methods)
        {
            // Get HTTP method from attributes
            var httpMethod = GetHttpMethod(method);
            if (httpMethod == null) continue;

            // Get route
            var route = GetRoute(controller, method);

            var actionInfo = new ActionInfo
            {
                ControllerName = controllerName,
                ActionName = method.Name,
                HttpMethod = httpMethod,
                Route = route,
                Description = GetDescription(method)
            };

            actionInfos.Add(actionInfo);
        }
    }

    await RegisterActionsAsync(actionInfos, cancellationToken);
}
```

### Permission Checking Strategy

**Effective Permission Calculation:**

1. Get all roles for user (from Identity)
2. Get all permissions for those roles
3. Get all user-specific permissions (not revoked, not expired)
4. Combine and deduplicate
5. Check if requested permission exists

**Caching Strategy:**
- Cache user permissions for 5-15 minutes
- Invalidate cache on permission changes
- Use memory cache or Redis

**SQL Query Example (PostgreSQL - Optimized):**
```sql
-- Get effective permissions for user
SELECT DISTINCT
    p."Id" AS "PermissionId",
    p."Name",
    p."Category",
    p."Description"
FROM "Permissions" p
WHERE p."IsActive" = true
  AND p."IsDeleted" = false
  AND p."Id" IN (
    -- Permissions from roles (using EXISTS for better performance)
    SELECT rp."PermissionId"
    FROM "RolePermissions" rp
    WHERE rp."IsActive" = true
      AND rp."IsDeleted" = false
      AND EXISTS (
        SELECT 1
        FROM "AspNetUserRoles" ur
        WHERE ur."RoleId" = rp."RoleId"
          AND ur."UserId" = @UserId
      )
    UNION
    -- User-specific permissions
    SELECT up."PermissionId"
    FROM "UserPermissions" up
    WHERE up."UserId" = @UserId
      AND up."IsRevoked" = false
      AND up."IsActive" = true
      AND up."IsDeleted" = false
      AND (up."ExpiresOn" IS NULL OR up."ExpiresOn" > CURRENT_TIMESTAMP)
  )
ORDER BY p."Category", p."Name";
```

**Note:** Use parameterized queries with Dapper/Npgsql. Replace `@UserId` with actual parameter binding (e.g., `new { UserId = userId }`). The query includes `IsDeleted = false` checks to respect soft-delete pattern.

---

## API Endpoints

### Permissions Management API

**Base Route:** `/api/v1/permissions`

```
GET    /api/v1/permissions                          - Get all permissions
GET    /api/v1/permissions/{id}                     - Get permission by ID
GET    /api/v1/permissions/category/{category}      - Get permissions by category
GET    /api/v1/permissions/sync                     - Manually trigger action discovery/sync

POST   /api/v1/permissions/roles/{roleId}           - Assign permission(s) to role
DELETE /api/v1/permissions/roles/{roleId}/{permissionId} - Revoke permission from role
GET    /api/v1/permissions/roles/{roleId}           - Get role permissions

POST   /api/v1/permissions/users/{userId}           - Assign permission(s) to user
DELETE /api/v1/permissions/users/{userId}/{permissionId} - Revoke permission from user
GET    /api/v1/permissions/users/{userId}           - Get user permissions
GET    /api/v1/permissions/users/{userId}/effective - Get user effective permissions (role + user)
```

### Request/Response DTOs

```csharp
// Permission DTO
public class PermissionDto
{
    public Guid Id { get; set; } // Changed from long to Guid (UUID)
    public string Name { get; set; }
    public string Category { get; set; }
    public string Description { get; set; }
    public Guid ActionId { get; set; } // Changed from long to Guid
    public string ControllerName { get; set; }
    public string ActionName { get; set; }
    public string HttpMethod { get; set; }
    public string Route { get; set; }
}

// Assign Permission Request
public class AssignPermissionRequestDto
{
    public List<Guid> PermissionIds { get; set; } // Changed from List<long> to List<Guid>
    public DateTime? ExpiresOn { get; set; } // For user permissions only
}

// User Effective Permissions Response
public class UserEffectivePermissionsDto
{
    public string UserId { get; set; }
    public List<string> Roles { get; set; }
    public List<PermissionDto> RolePermissions { get; set; }
    public List<PermissionDto> UserPermissions { get; set; }
    public List<PermissionDto> AllPermissions { get; set; } // Combined and deduplicated
}
```

---

## Authorization Flow

### Flow Diagram

```
Request → Authentication → Authorization Check
                              ↓
                    [RequirePermission] attribute?
                              ↓
                    Yes: Check Permission
                              ↓
                    User has permission?
                              ↓
                    Yes: Allow    No: 403 Forbidden
```

### Permission Check Process

1. **Request arrives** at controller action
2. **Authorization middleware** checks for `[RequirePermission]` attribute
3. **PermissionAuthorizationHandler** is invoked
4. **PermissionService** is called to check permission
5. **Cache lookup** for user permissions
6. **Database query** if not cached (role permissions + user permissions)
7. **Result** returned (allow/deny)
8. **Response** sent (200 OK or 403 Forbidden)

### Multiple Authorization Methods Support

**Option 1: Permission-Based (Recommended)**
```csharp
[RequirePermission("Users.GetAll")]
public async Task<IActionResult> GetAllUsers() { ... }
```

**Option 2: Role-Based (Existing)**
```csharp
[Authorize(Roles = "Admin,SuperAdmin")]
public async Task<IActionResult> GetAllUsers() { ... }
```

**Option 3: Hybrid (Both)**
```csharp
[Authorize(Roles = "Admin,SuperAdmin")]
[RequirePermission("Users.GetAll")]
public async Task<IActionResult> GetAllUsers() { ... }
```

---

## Migration Strategy

### Phase 1: Database Setup
1. Create migration for new tables
2. Run migration
3. Seed pre-defined roles (Staff, Admin, Driver, etc.)

### Phase 2: Action Discovery
1. Implement `ActionDiscoveryService`
2. Run discovery on startup
3. Verify all actions are registered

### Phase 3: Permission Creation
1. Auto-create permissions for all actions
2. Manually review and organize permissions
3. Assign permissions to pre-defined roles

### Phase 4: Authorization Integration
1. Add `PermissionAuthorizationHandler` to DI
2. Configure authorization policies
3. Add `[RequirePermission]` attributes to existing controllers (optional, gradual)

### Phase 5: Admin Interface
1. Create admin API endpoints
2. Build admin UI for permission management
3. Test permission assignment/revocation

### Phase 6: Rollout
1. Deploy to development
2. Test with test users
3. Deploy to production
4. Monitor performance

---

## Security Considerations

### 1. Permission Escalation Prevention
- Only SuperAdmin/Developer roles can assign permissions
- Audit all permission changes
- Prevent users from granting permissions to themselves

### 2. Performance
- Cache user permissions (5-15 min TTL)
- Use indexes on foreign keys
- Consider Redis for distributed caching

### 3. Data Integrity
- Foreign key constraints
- Cascade deletes where appropriate
- Soft delete for audit trail (mark inactive, don't delete)

### 4. Audit Trail
- Log all permission changes
- Track who granted/revoked permissions
- Track when permissions were changed

### 5. Default Permissions
- New actions should be inactive by default (opt-in security)
- Or assign to SuperAdmin/Developer only by default

---

## Pre-Defined Roles and Permissions

### Suggested Role Structure

**SuperAdmin**
- All permissions
- Cannot be modified
- Can manage all permissions

**Developer**
- All permissions except system-critical
- Can manage permissions
- Cannot modify SuperAdmin role

**Admin**
- User management (create, update, delete)
- Role management (limited)
- Permission viewing
- Cannot manage SuperAdmin/Developer permissions

**Staff**
- View users
- View roles
- Limited to read operations

**Driver** (example)
- View assigned tasks
- Update own status
- Limited to driver-specific actions

### Permission Categories

- **Users**: Users.GetAll, Users.Create, Users.Update, Users.Delete
- **Roles**: Roles.GetAll, Roles.Create, Roles.Update, Roles.Delete
- **Permissions**: Permissions.View, Permissions.Manage
- **System**: System.Configure, System.Admin

---

## Example Usage Scenarios

### Scenario 1: Pre-Defined Role Assignment

```csharp
// Admin user "John" gets Staff role
// Staff role has: Users.View, Roles.View permissions
// John can: View users and roles

// Code:
await _userManager.AddToRoleAsync(john, "Staff");
// Permissions are automatically inherited from Staff role
```

### Scenario 2: User-Specific Permission Override

```csharp
// Staff user "Jane" gets Staff role
// Staff role has: Users.View, Roles.View
// Jane also gets: Users.Create (user-specific permission)
// Jane can: View users/roles (from role) + Create users (user-specific)

// Code:
await _userManager.AddToRoleAsync(jane, "Staff");
await _permissionService.AssignPermissionToUserAsync(
    jane.Id,
    permissionId: Guid.Parse("f6a7b8c9-d0e1-4f2a-3b4c-5d6e7f8a9b0c"), // Users.Create (UUID)
    grantedBy: adminUserId);
```

### Scenario 3: Creating Custom Role

```csharp
// Create "Manager" role
var managerRole = new IdentityRole("Manager");
await _roleManager.CreateAsync(managerRole);

// Assign permissions to Manager role
var userPermissionIds = new List<Guid>
{
    Guid.Parse("e5f6a7b8-c9d0-4e1f-2a3b-4c5d6e7f8a9b"), // Users.GetAll
    Guid.Parse("f6a7b8c9-d0e1-4f2a-3b4c-5d6e7f8a9b0c"), // Users.Create
    Guid.Parse("a7b8c9d0-e1f2-4a3b-4c5d-6e7f8a9b0c1d"), // Users.Update
    Guid.Parse("b8c9d0e1-f2a3-4b4c-5d6e-7f8a9b0c1d2e")  // Users.Delete
};
await _permissionService.AssignMultiplePermissionsToRoleAsync(
    managerRole.Id,
    permissionIds: userPermissionIds,
    grantedBy: adminUserId);

// Assign user to Manager role
await _userManager.AddToRoleAsync(user, "Manager");
```

---

## Performance Optimization

### 1. Caching Strategy

```csharp
// Cache user permissions
public class PermissionCacheService
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(10);

    public async Task<List<PermissionDto>> GetUserPermissionsAsync(
        string userId,
        Func<Task<List<PermissionDto>>> fetchFromDb)
    {
        var cacheKey = $"UserPermissions:{userId}";

        if (_cache.TryGetValue(cacheKey, out List<PermissionDto> cached))
        {
            return cached;
        }

        var permissions = await fetchFromDb();
        _cache.Set(cacheKey, permissions, _cacheExpiration);
        return permissions;
    }

    public void InvalidateUserPermissions(string userId)
    {
        _cache.Remove($"UserPermissions:{userId}");
    }
}
```

### 2. Database Optimization

- Indexes on foreign keys
- Indexes on frequently queried columns
- Consider materialized views for complex queries
- Partition PermissionAuditLog table by date (if large)

### 3. Lazy Loading

- Load permissions on-demand
- Don't load all permissions at startup
- Use background job for permission sync

---

## Testing Strategy

### Unit Tests
- Action discovery service
- Permission service methods
- Authorization handler
- Permission calculation logic

### Integration Tests
- Permission assignment/revocation
- Effective permission calculation
- Authorization flow
- API endpoints

### Performance Tests
- Permission check performance (< 10ms)
- Cache hit rates
- Database query performance
- Load testing with many permissions

---

## Future Enhancements

### Phase 2 Features (Optional)

1. **Permission Groups**
   - Group related permissions
   - Assign groups to roles/users

2. **Time-Limited Permissions**
   - Permissions with expiration dates
   - Temporary access grants

3. **Permission Inheritance**
   - Hierarchical role structure
   - Inherit permissions from parent roles

4. **Resource-Based Permissions**
   - Permissions on specific resources (e.g., "Users.Update:User123")
   - Fine-grained access control

5. **Conditional Permissions**
   - Permissions based on conditions (e.g., "Users.Update:OwnOnly")

6. **Permission Templates**
   - Pre-defined permission sets
   - Quick role setup

---

## Questions for Discussion

1. **Should we extend AspNetRoles or create a separate CustomRoles table?**
   - Recommendation: Extend with RoleDetails table for metadata

2. **Should permissions be active by default when actions are discovered?**
   - Recommendation: Inactive by default (opt-in security)

3. **Should we support permission inheritance in roles?**
   - Recommendation: Phase 2 feature

4. **What is the caching strategy?**
   - Recommendation: Memory cache with 10-minute TTL

5. **Should we support resource-level permissions?**
   - Recommendation: Phase 2 feature

6. **How to handle permission conflicts?**
   - Recommendation: User permissions override role permissions

7. **Should we migrate existing [Authorize(Roles)] attributes?**
   - Recommendation: Keep both, gradual migration

---

## Implementation Checklist

- [ ] Design review and approval
- [ ] Database schema design finalization
- [ ] Create EF Core entities
- [ ] Create database migration
- [ ] Implement repositories
- [ ] Implement ActionDiscoveryService
- [ ] Implement PermissionService
- [ ] Implement authorization handler
- [ ] Create RequirePermission attribute
- [ ] Create admin API endpoints
- [ ] Add permission caching
- [ ] Write unit tests
- [ ] Write integration tests
- [ ] Performance testing
- [ ] Documentation
- [ ] Admin UI (separate task)

---

## Integration with Existing System

### Current System Analysis

The existing system uses:
- **ASP.NET Core Identity** with `AppUser` and `IdentityRole`
- **RoleManager<IdentityRole>** for role management
- **UserManager<AppUser>** for user management
- **PostgreSQL** database
- **Dapper** with repository pattern (`RepositoryBasePostgreSql`)
- **DTO-based API** with `GenericResponseDto<T>`
- **UserContextService** with role access maps

### Integration Points

1. **Identity Integration**
   - Leverage existing `AspNetRoles` table (extend with `RoleDetails` table)
   - Use existing `AspNetUserRoles` for user-role assignments
   - Keep existing role management services
   - Add permission layer on top of roles

2. **Repository Pattern**
   - Follow existing pattern: `RepositoryBasePostgreSql`
   - Create repositories: `ActionRepository`, `PermissionRepository`, `UserPermissionRepository`
   - Implement interfaces in `BachatCommittee.Data.Repos.Interfaces`
   - Register in `ServicesCollection.AddBachatCommitteeRepos`

3. **Service Layer**
   - Create `PermissionService` following existing service patterns
   - Use async/await with `ConfigureAwait(false)` pattern
   - Implement caching similar to `RoleManagementService`
   - Follow cancellation token patterns

4. **DTO Patterns**
   - Create DTOs in `BachatCommittee.Models.DTOs.Requests/Responses`
   - Use `GenericResponseDto<T>` for API responses
   - Follow existing validation patterns

5. **Authorization Integration**
   - Integrate with existing `[Authorize(Roles = "...")]` attributes
   - Add `[RequirePermission]` attribute as additional layer
   - Keep `UserContextService` role checks for backward compatibility
   - Add permission checks alongside role checks

### Backward Compatibility

The permission system is designed to be **additive** and **non-breaking**:

- Existing `[Authorize(Roles = "...")]` attributes continue to work
- Role-based authorization remains functional
- Permission system adds additional authorization layer
- Can be adopted gradually (action-by-action or controller-by-controller)
- Existing roles (Staff, Admin, Developer, SuperAdmin, Customer) remain valid

### Migration from Role-Based to Permission-Based

**Recommended Approach:**
1. Start with new controllers/actions using permissions
2. Gradually migrate high-value endpoints
3. Keep role-based authorization for simple use cases
4. Use hybrid approach (roles + permissions) where needed

**Example Migration:**
```csharp
// Before: Role-based only
[Authorize(Roles = "Admin,SuperAdmin")]
public async Task<IActionResult> GetAllUsers() { ... }

// After Option 1: Permission-based
[RequirePermission("Users.GetAll")]
public async Task<IActionResult> GetAllUsers() { ... }

// After Option 2: Hybrid (both required)
[Authorize(Roles = "Admin,SuperAdmin")]
[RequirePermission("Users.GetAll")]
public async Task<IActionResult> GetAllUsers() { ... }

// After Option 3: Permission OR Role
// Custom logic in authorization handler
```

---

## Implementation Checklist

- [ ] Design review and approval
- [ ] Database schema design finalization
- [ ] Create EF Core entities
- [ ] Create database migration (FluentMigrator)
- [ ] Implement repositories (ActionRepository, PermissionRepository, UserPermissionRepository)
- [ ] Implement repository interfaces
- [ ] Register repositories in DI
- [ ] Implement ActionDiscoveryService
- [ ] Implement PermissionService
- [ ] Implement permission caching service
- [ ] Create authorization handler
- [ ] Create RequirePermission attribute
- [ ] Register authorization handler in Program.cs
- [ ] Create DTOs (PermissionDto, AssignPermissionRequestDto, etc.)
- [ ] Create admin API endpoints (PermissionsController)
- [ ] Add action discovery to startup
- [ ] Write unit tests
- [ ] Write integration tests
- [ ] Performance testing
- [ ] Documentation
- [ ] Admin UI (separate task)

---

*Document Version: 1.0*
*Last Updated: [Current Date]*
*Status: Draft for Review*

