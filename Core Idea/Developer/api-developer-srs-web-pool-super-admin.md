# API Developer SRS  
## Web Pool – Super Admin  
**Document Type:** Software Requirements Specification (API-Level)  
**Audience:** Backend / API Developers  
**Application Layer:** Web Pool (Super Admin)  
**Architecture Style:** RESTful API (Stateless)  
**Version:** 1.1  
**Status:** Final – Developer Ready  

---

## 1. Purpose of This Document

This document defines **complete backend/API requirements** for the **Web Pool – Super Admin** system. It is the single source of truth for:

- API contract definitions
- Authorization & security rules
- Validation, idempotency, and edge cases
- Workflow orchestration and background processing
- Error handling (standard envelope)
- Performance and non-functional requirements

This document intentionally **does not cover UI/UX**.

---

## 2. System Overview

The **Web Pool – Super Admin** is the highest-privileged control layer responsible for:

- System-wide configuration
- Tenant lifecycle management
- User governance across all pools/tenants
- Rules, thresholds, and automation orchestration
- Audit, monitoring, and enforcement

The system follows a **multi-tenant, role-based access control (RBAC)** model.

---

## 3. Roles & Access Scope

### 3.1 Role Hierarchy

| Role | Scope | Notes |
|---|---|---|
| SuperAdmin | Global | Unrestricted (subject to audit) |
| PoolAdmin | Tenant-bound | Restricted to assigned tenant(s) |
| Auditor | Read-only | No mutations |
| Support | Limited write | Controlled operations |

**Rule:** Super Admin endpoints MUST NOT be accessible by any lower role.

---

## 4. Authentication & Authorization

### 4.1 Authentication

- Protocol: **JWT Bearer**
- Token Source: `Authorization: Bearer <token>`
- Token Lifetime: configurable (default 60 minutes)
- Refresh Token: required for session continuation
- Revocation: supported (force logout / lock / tenant suspend)

**Required JWT claims (minimum):**
```json
{
  "sub": "userId",
  "role": "SuperAdmin",
  "tenant_scope": "global",
  "permissions": ["*"],
  "iat": 1700000000,
  "exp": 1700003600
}
```

### 4.2 Authorization

- Role-based AND permission-based checks (policy-based authorization)
- Explicit deny if permission missing
- Audit trail required for privileged actions

---

## 5. Standard API Response Envelope (MANDATORY)

All endpoints MUST return `GenericReponseDto<T>` (including errors).

### 5.1 GenericReponseDto<T>

```json
{
  "statusCode": 200,
  "message": "Success",
  "response": {}
}
```

### 5.2 Validation Error

```json
{
  "statusCode": 400,
  "message": "Validation failed",
  "response": {
    "errors": {
      "field": "Reason"
    }
  }
}
```

### 5.3 Auth Errors

- 401:
```json
{ "statusCode": 401, "message": "Unauthorized", "response": null }
```
- 403:
```json
{ "statusCode": 403, "message": "Insufficient privileges", "response": null }
```

### 5.4 Not Found

```json
{ "statusCode": 404, "message": "Not found", "response": null }
```

### 5.5 Server Error

```json
{ "statusCode": 500, "message": "Internal server error", "response": { "traceId": "..." } }
```

---

## 6. Core Domain: Tenants

### 6.1 Create Tenant

**POST** `/api/v1/super-admin/tenants`

**Request:**
```json
{
  "name": "Tenant Name",
  "code": "TNT001",
  "timezone": "UTC",
  "currency": "USD",
  "isActive": true
}
```

**Validations:**
- `code` unique, `A-Z0-9-_` only, length 3–20
- `timezone` must be valid IANA or supported list
- `currency` ISO-4217
- `name` length >= 3

**Edge cases:**
- Duplicate tenant code -> 409
- If downstream provisioning fails -> transaction rollback + audit + compensating cleanup
- If `isActive=false` on creation -> tenant created but authentication disabled

**Success (201):**
```json
{
  "statusCode": 201,
  "message": "Tenant created",
  "response": { "tenantId": "uuid", "code": "TNT001" }
}
```

### 6.2 List Tenants (paged)

**GET** `/api/v1/super-admin/tenants?page=1&pageSize=50&search=...&isActive=true`

Rules:
- Pagination mandatory
- `pageSize` max 100

### 6.3 Get Tenant Detail

**GET** `/api/v1/super-admin/tenants/{tenantId}`

### 6.4 Update Tenant

**PUT** `/api/v1/super-admin/tenants/{tenantId}`

Rules:
- Code immutable (recommended) OR allow change with strict migration policy (must be decided; default: immutable)
- Updating timezone may require re-scheduling background jobs

### 6.5 Suspend / Activate Tenant

**PATCH** `/api/v1/super-admin/tenants/{tenantId}/status`

**Request:**
```json
{ "isActive": false, "reason": "Compliance hold" }
```

Behavior when suspended:
- Cannot authenticate (login denied)
- Refresh tokens revoked
- Webhooks disabled
- Background jobs halted (except audit/cleanup)

### 6.6 Delete Tenant (soft delete)

**DELETE** `/api/v1/super-admin/tenants/{tenantId}?confirm=true`

Constraints:
- Soft delete only
- Data retained for audit
- Must fail if `confirm!=true`

---

## 7. Global User Governance

### 7.1 Global User Search (paged)

**GET** `/api/v1/super-admin/users?email=...&role=...&tenantId=...&status=...&page=1&pageSize=50`

Rules:
- Pagination mandatory
- Supports partial email search (case-insensitive)

### 7.2 Get User Detail

**GET** `/api/v1/super-admin/users/{userId}`

### 7.3 Force Logout User

**POST** `/api/v1/super-admin/users/{userId}/logout`

Behavior:
- Revoke refresh tokens for user
- Add token version bump (if supported)
- Log audit entry with actor + reason

### 7.4 Lock / Unlock User

**PATCH** `/api/v1/super-admin/users/{userId}/lock`

Request:
```json
{ "isLocked": true, "reason": "Multiple fraud attempts" }
```

Rules:
- Lock denies login and refresh
- Unlock restores access (unless tenant suspended)

---

## 8. Roles & Permissions

### 8.1 Create Role

**POST** `/api/v1/super-admin/roles`

Request:
```json
{
  "name": "CustomRole",
  "permissions": ["ORDERS_VIEW", "ORDERS_EDIT"]
}
```

Validations:
- Name unique
- Permissions must exist

### 8.2 List Roles

**GET** `/api/v1/super-admin/roles?page=1&pageSize=50`

### 8.3 Update Role Permissions

**PUT** `/api/v1/super-admin/roles/{roleId}`

Rules:
- Changes versioned in audit logs
- Do not allow wildcard for non-super roles

---

## 9. Configuration Management

### 9.1 Global Configuration

**GET** `/api/v1/super-admin/configurations/global`  
**PUT** `/api/v1/super-admin/configurations/global`

Examples:
- Password policy
- Session timeout
- Rate limits
- Feature toggles

Rules:
- PUT should support partial updates via JSON Patch OR accept full model (default: full model)
- Configuration changes should be audited with before/after snapshot

### 9.2 Tenant Overrides

**GET** `/api/v1/super-admin/configurations/tenants/{tenantId}`  
**PUT** `/api/v1/super-admin/configurations/tenants/{tenantId}`

Rules:
- Overrides only permitted for keys marked `Overridable=true`

---

## 10. Rules & Automation Engine

### 10.1 Create Rule

**POST** `/api/v1/super-admin/rules`

```json
{
  "name": "AutoSuspendRule",
  "description": "Suspend user after 5 failed logins in 10 minutes",
  "scope": "global",
  "isEnabled": true,
  "condition": "failed_logins_10m > 5",
  "action": "SUSPEND_USER"
}
```

Rules:
- Rule name unique per scope
- Condition language must be validated (compile check) before saving
- Actions must be in allowed enum

### 10.2 List Rules

**GET** `/api/v1/super-admin/rules?page=1&pageSize=50&scope=global&isEnabled=true`

### 10.3 Enable/Disable Rule

**PATCH** `/api/v1/super-admin/rules/{ruleId}/status`

### 10.4 Execute Rule (admin-triggered, optional)

**POST** `/api/v1/super-admin/rules/{ruleId}/execute?dryRun=true`

Behavior:
- `dryRun=true` returns impacted entities without mutating
- Must be rate-limited & audited

### 10.5 Execution Guarantees

- Must be idempotent
- Async processing via queue
- Retry: max 3, exponential backoff
- Dead-letter queue for failures

---

## 11. Audit & Observability

### 11.1 Audit Event Coverage (Minimum)

- Login / logout / refresh / revoke
- Tenant create / update / status / delete
- Role / permission changes
- Config changes
- Rule create / update / status / execute
- User lock/unlock/force logout

### 11.2 Audit Log Retrieval

**GET** `/api/v1/super-admin/audit-logs?page=1&pageSize=50&actorId=...&entityType=...&dateFrom=...&dateTo=...`

Rules:
- Immutable records
- PII redaction policy enforced

---

## 12. Rate Limiting & Throttling

Default limits (configurable):

| Scope | Limit |
|---|---|
| Super Admin general APIs | 1000 req/min |
| Config APIs | 100 req/min |
| Rule execution APIs | 30 req/min |

On breach: return 429 with retry hints.

---

## 13. Failure & Recovery (Hard Requirements)

| Scenario | Handling |
|---|---|
| DB outage | Circuit breaker + 503 |
| Partial writes | Transactions + rollback |
| Queue failure | Persist job record + retry |
| External dependency down | Retry + DLQ + alert |
| Duplicate request (client retry) | Idempotency keys (where applicable) |

Idempotency guidance:
- For create endpoints: accept `Idempotency-Key` header (recommended)
- Store key + response for a window (e.g., 24 hours)

---

## 14. Security Requirements (Hard Requirements)

- HTTPS mandatory
- Parameterized SQL / ORM-safe queries
- Secrets in vault / env vars (no plaintext in repo)
- OWASP logging hygiene (no passwords/tokens)
- Strict CORS for admin origins
- Admin Swagger must be auth-protected

---

## 15. Performance Requirements

- P95 < 500ms for CRUD endpoints (under normal load)
- Pagination mandatory for lists
- Max `pageSize`: 100
- Support DB indexing for search fields (email, tenantId, code, createdOn)

---

## 16. API Versioning

- URL versioning: `/api/v1`
- Breaking changes require new major version
- Deprecation window: 90 days

---

## 17. API Documentation

- OpenAPI (Swagger)
- Role-aware endpoint visibility (optional)
- Example payloads for each endpoint

---

## 18. Completion Criteria

- All endpoints implemented and integration-tested
- Security policies enforced and verified
- Audit logging validated for all privileged actions
- Performance benchmarks met
- OpenAPI + Postman collection published

---

**End of Document**
