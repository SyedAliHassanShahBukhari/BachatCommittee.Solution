# API Developer SRS — Pool Admin (Web Admin) — v1.0

> **Scope**: This document defines the **Pool Admin** back-end API requirements (ASP.NET Core) with implementation-grade detail: endpoints, DTOs, validation, permissions, workflows, errors, auditing, and integration touchpoints.
>
> **Audience**: API developers, backend engineers, QA (API-level), DevOps.
>
> **Conventions**:
> - All responses use `GenericReponseDto<T>`.
> - REST endpoints are versioned: `/api/v1/...`
> - Authentication: JWT Bearer (primary). Optional API key headers supported where noted.
> - All timestamps are **UTC**; store as `timestamptz` (PostgreSQL).

---

## 1. Definitions

### 1.1 Roles (minimum)
- **PoolAdmin**
- **PoolSuperAdmin** (higher privileges, can manage pool admins and pool settings)
- **Support** (read-only + limited actions)
- **Developer** (internal; full access, audit-only constraints)

> The platform may have additional global roles (TenantAdmin, SuperAdmin). Pool Admin APIs must enforce **role + scope** (pool/tenant) constraints.

### 1.2 Tenancy and Scoping
- All Pool Admin data must be scoped by:
  - `TenantId` (required)
  - `PoolId` (required where multi-pool is supported)
- JWT must include claims:
  - `sub` (UserId)
  - `tenant_id`
  - `pool_id` (if user is assigned to a pool)
  - `roles` / `role`

### 1.3 Standard Response DTO (mandatory)
```csharp
public sealed class GenericReponseDto<T>
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Response { get; set; }
}
```

### 1.4 Ids and Correlation
- Every request must accept/propagate:
  - `X-Correlation-Id` (optional; if absent generate server-side)
- Every response must include the correlation id header.
- Log with structured fields: `CorrelationId`, `TenantId`, `PoolId`, `UserId`, `Endpoint`, `StatusCode`.

---

## 2. Non-Functional Requirements

### 2.1 Performance
- 95th percentile latency:
  - Read endpoints: <= 250ms (excluding external providers)
  - Write endpoints: <= 500ms
- Pagination is mandatory for list endpoints.

### 2.2 Security
- JWT validation (issuer, audience, lifetime, signing key)
- Role-based access control (RBAC) + resource scoping
- Rate limits (recommended): per-tenant and per-user
- Input validation:
  - reject unknown enum values
  - string max lengths and sanitization for logs
- PII masking in logs.

### 2.3 Observability
- Structured logging (Serilog or equivalent)
- Metrics:
  - request duration by route
  - error rate by route/status code
  - DB query duration
- Audit trail for privileged actions.

### 2.4 Compatibility / Versioning
- Versioned routes: `/api/v1`
- Backward-compatible changes only within v1.
- Breaking changes must go to `/api/v2`.

---

## 3. Domain Model (Pool Admin)

> The following entities are referenced by API. Exact schema can vary, but the API contract must remain stable.

### 3.1 Pool
- `Id (uuid)`
- `TenantId (uuid)`
- `Name (varchar 150)`
- `Code (varchar 50)` unique per tenant
- `TimeZone (varchar 80)` IANA TZ (optional)
- `IsActive (bool)`

### 3.2 Member (User within pool)
- `Id (uuid)` (or Identity UserId)
- `TenantId (uuid)`
- `PoolId (uuid)`
- `FullName (varchar 150)`
- `Phone (varchar 30)` (unique per tenant optional)
- `Email (varchar 255)`
- `RoleInPool (enum)` e.g., Member, Receiver
- `IsActive (bool)`
- `IsSuspended (bool)`
- `JoinedOnUtc (timestamptz)`

### 3.3 Committee Cycle / Month
- `Id (uuid)`
- `TenantId (uuid)`
- `PoolId (uuid)`
- `CycleName (varchar 150)`
- `StartMonth (date)` (first day)
- `EndMonth (date)` (optional)
- `Status (enum)` Draft, Active, Closed, Archived

### 3.4 Contribution
- `Id (uuid)`
- `TenantId (uuid)`
- `PoolId (uuid)`
- `CycleId (uuid)`
- `MemberId (uuid)`
- `Month (date)` (first day)
- `Amount (numeric(18,2))`
- `PaymentStatus (enum)` Pending, Paid, Partial, Overdue, Waived
- `PaymentMethod (enum)` Cash, Bank, Wallet, Online
- `PaidOnUtc (timestamptz?)`
- `ReferenceNo (varchar 80?)`
- `Notes (varchar 500?)`

### 3.5 Draw / Winner Allocation
- `Id (uuid)`
- `TenantId (uuid)`
- `PoolId (uuid)`
- `CycleId (uuid)`
- `Month (date)`
- `WinnerMemberId (uuid)`
- `DrawMethod (enum)` Manual, Random
- `DrawNumber (int)` (if random)
- `DrawnBy (uuid)` (admin user id)
- `DrawnOnUtc (timestamptz)`
- `PayoutStatus (enum)` Pending, Released, Cancelled
- `PayoutRef (varchar 80?)`

### 3.6 Audit Log
- `Id (uuid)`
- `TenantId (uuid)`
- `PoolId (uuid?)`
- `ActorUserId (uuid)`
- `Action (varchar 80)`
- `EntityType (varchar 80)`
- `EntityId (uuid?)`
- `BeforeJson (jsonb?)`
- `AfterJson (jsonb?)`
- `CreatedOnUtc (timestamptz)`
- `CorrelationId (varchar 80)`

---

## 4. Authorization Matrix (Pool Admin)

| Feature | PoolAdmin | PoolSuperAdmin | Support | Developer |
|---|---:|---:|---:|---:|
| View dashboard metrics | Yes | Yes | Yes | Yes |
| Manage members | Yes (scoped) | Yes | Read-only | Yes |
| Create/activate cycle | Yes | Yes | No | Yes |
| Record contributions | Yes | Yes | Yes (restricted) | Yes |
| Run monthly draw | Yes | Yes | No | Yes |
| Mark payout released | Yes | Yes | No | Yes |
| Edit past months | Restricted (config) | Yes | No | Yes |
| Export reports | Yes | Yes | Yes | Yes |
| Manage pool settings | No | Yes | No | Yes |
| View audit logs | Limited | Full | Limited | Full |

**Key rules**
- PoolAdmin can only act within their assigned `PoolId`.
- Support must never mutate cycle/draw/payout states (except adding notes if allowed).
- Developer actions must still write audit logs.

---

## 5. API Standards

### 5.1 Headers
- `Authorization: Bearer <token>` (required)
- `X-Correlation-Id: <guid-or-string>` (optional)
- Optional (if enabled): `x-api-key`, `x-api-secret`

### 5.2 Paging
List endpoints must support:
- `page` (1-based, default 1)
- `pageSize` (default 25, max 200)
- `sortBy` (field)
- `sortDir` (`asc|desc`)
- `q` (search query, optional)

Response wrapper:
```json
{
  "statusCode": 200,
  "message": "OK",
  "response": {
    "items": [],
    "page": 1,
    "pageSize": 25,
    "totalItems": 0,
    "totalPages": 0
  }
}
```

### 5.3 Error Handling
- **400** validation issues
- **401** unauthorized (missing/invalid token)
- **403** forbidden (role/scope)
- **404** not found (scoped entity)
- **409** conflict (state transitions)
- **422** business rule violated (optional; if you prefer keep 400)
- **500** unexpected

Error response (standard):
```json
{
  "statusCode": 400,
  "message": "Validation failed",
  "response": {
    "errors": [
      { "field": "amount", "message": "Amount must be greater than 0" }
    ]
  }
}
```

### 5.4 Idempotency (writes)
For POST operations that can be retried:
- Accept optional header `Idempotency-Key` (recommended)
- Store key + response hash for N hours (e.g., 24h) per tenant.

---

## 6. Core Modules and Endpoints

> All routes are prefixed with `/api/v1/pool-admin` unless explicitly stated.

### 6.1 Auth / Session (read-only for pool admin app)
Pool admin UI typically uses the platform auth module. These endpoints are included for completeness.

#### 6.1.1 Get My Profile
- `GET /api/v1/auth/me`
- **Auth**: Any authenticated user
- **Response**: `MeResponseDto`

```json
{
  "statusCode": 200,
  "message": "OK",
  "response": {
    "userId": "uuid",
    "tenantId": "uuid",
    "poolId": "uuid",
    "fullName": "Ali Hassan",
    "email": "admin@domain.com",
    "roles": ["PoolAdmin"]
  }
}
```

---

### 6.2 Dashboard

#### 6.2.1 Summary Metrics
- `GET /api/v1/pool-admin/dashboard/summary?cycleId=<uuid?>&month=<yyyy-mm-01?>`
- **Auth**: PoolAdmin+
- **Rules**:
  - If `cycleId` not provided, use **Active** cycle.
  - If `month` not provided, use current month in pool timezone (fallback UTC).
- **Response**: `DashboardSummaryResponseDto`

**Fields**
- `activeCycleId`
- `membersCount`
- `paidCount`, `pendingCount`, `overdueCount`
- `expectedCollectionAmount`
- `receivedCollectionAmount`
- `payoutStatus` for month
- `lastDrawInfo` (optional)

#### 6.2.2 Recent Activities
- `GET /api/v1/pool-admin/dashboard/activities?page=1&pageSize=25`
- Source: AuditLog + domain events
- Must redact sensitive fields.

---

### 6.3 Members Management

#### 6.3.1 List Members
- `GET /api/v1/pool-admin/members?page=1&pageSize=25&q=<search>&status=<active|inactive|suspended|all>`
- **Auth**: PoolAdmin+
- **Sort**: `fullName`, `joinedOnUtc`, `status`

Response item (`MemberListItemDto`):
- `memberId`
- `fullName`
- `phone`
- `email`
- `roleInPool`
- `isActive`
- `isSuspended`
- `joinedOnUtc`

#### 6.3.2 Create Member
- `POST /api/v1/pool-admin/members`
- **Auth**: PoolAdmin+
- **Request**: `CreateMemberRequestDto`
- **Validations**
  - `fullName` required, 2..150
  - at least one of `phone` or `email`
  - phone normalization (E.164 recommended)
  - email unique per tenant (if configured)
- **Business Rules**
  - Member cannot be added to a non-active pool.
  - If user already exists globally: link to pool membership; do not duplicate user.
- **Audit**: `Member.Created` (Before/After)

#### 6.3.3 Update Member
- `PUT /api/v1/pool-admin/members/{memberId}`
- **Rules**
  - immutable fields: `tenantId`, `poolId`
  - optional: disallow changing `roleInPool` if member already won payout (configurable)
- **Audit**: `Member.Updated`

#### 6.3.4 Suspend/Unsuspend Member
- `POST /api/v1/pool-admin/members/{memberId}/suspend`
- `POST /api/v1/pool-admin/members/{memberId}/unsuspend`
- **Rules**
  - Suspended members cannot be selected as draw winners.
  - Suspended members can still have historical contributions.
- **Audit**: `Member.Suspended`, `Member.Unsuspended`

#### 6.3.5 Deactivate/Reactivate
- `POST /api/v1/pool-admin/members/{memberId}/deactivate`
- `POST /api/v1/pool-admin/members/{memberId}/activate`
- **Rules**
  - Deactivated members do not appear in default lists.
  - Deactivation blocked if there are unpaid obligations in active cycle (optional rule).
- **Audit**: `Member.Deactivated`, `Member.Activated`

---

### 6.4 Cycles (Committee Months)

#### 6.4.1 List Cycles
- `GET /api/v1/pool-admin/cycles?page=1&pageSize=25&status=<draft|active|closed|all>`

#### 6.4.2 Create Cycle
- `POST /api/v1/pool-admin/cycles`
- **Request**: `CreateCycleRequestDto`
  - `cycleName` (required)
  - `startMonth` (required, yyyy-mm-01)
  - `expectedMonthlyShareAmount` (numeric>0) (optional if share per member varies)
  - `membersSnapshotMode` (enum): Live, SnapshotOnActivation
- **Rules**
  - Only one **Active** cycle per pool at a time (default).
  - StartMonth must be >= current month - configurable (e.g., cannot start 12 months in past).
- **Audit**: `Cycle.Created`

#### 6.4.3 Activate Cycle
- `POST /api/v1/pool-admin/cycles/{cycleId}/activate`
- **Rules**
  - Must be Draft.
  - Must have >= minimum members (configurable).
  - If Snapshot mode enabled: create member snapshot records.
  - Generate contribution schedule rows for each month/member if you precompute.
- **Conflict**: If another cycle is active => 409.
- **Audit**: `Cycle.Activated`

#### 6.4.4 Close Cycle
- `POST /api/v1/pool-admin/cycles/{cycleId}/close`
- **Rules**
  - Must be Active.
  - Cannot close if any payout pending or contributions unpaid (configurable).
- **Audit**: `Cycle.Closed`

#### 6.4.5 Cycle Details
- `GET /api/v1/pool-admin/cycles/{cycleId}`
- Includes months, member counts, total amounts.

---

### 6.5 Contributions (Payments)

#### 6.5.1 List Contributions (by month)
- `GET /api/v1/pool-admin/contributions?cycleId=<uuid>&month=<yyyy-mm-01>&status=<paid|pending|overdue|all>&page=1&pageSize=50&q=<member>`
- **Rules**
  - `cycleId` required; if missing use active cycle.
  - `month` required; if missing default current month.
- **Response**: ContributionListDto with items and summary totals.

#### 6.5.2 Record Payment (single)
- `POST /api/v1/pool-admin/contributions/record`
- **Request**: `RecordContributionRequestDto`
  - `cycleId`
  - `month`
  - `memberId`
  - `amount`
  - `paymentMethod`
  - `paidOnUtc` (optional; default now)
  - `referenceNo` (optional)
  - `notes` (optional)
- **Validation**
  - amount > 0
  - month within cycle range (if defined)
- **Rules**
  - If contribution already paid and edit locked => 409.
  - If partial payments allowed: update status `Partial` and store payment ledger.
- **Audit**: `Contribution.Recorded`

#### 6.5.3 Bulk Record Payments
- `POST /api/v1/pool-admin/contributions/bulk-record`
- Accepts array of `RecordContributionRequestDto`
- **Rules**
  - Must be transactional per request (all-or-nothing) OR per-item with per-item results (choose one; recommended per-item results).
- **Response**: `BulkOperationResultDto` containing successes and failures with error messages.

#### 6.5.4 Mark Overdue / Auto Overdue
- `POST /api/v1/pool-admin/contributions/{contributionId}/mark-overdue`
- Typically a nightly job sets overdue based on due date rules.
- Manual override must be audited.

#### 6.5.5 Waive Payment (rare)
- `POST /api/v1/pool-admin/contributions/{contributionId}/waive`
- **Rules**
  - Only PoolSuperAdmin or PoolAdmin if config permits.
  - Requires `reason` in request.
- **Audit**: `Contribution.Waived`

#### 6.5.6 Edit Payment
- `PUT /api/v1/pool-admin/contributions/{contributionId}`
- **Rules**
  - Edits allowed only within configured window (e.g., 72 hours) unless PoolSuperAdmin.
  - Must preserve original payment ledger for audit (do not overwrite blindly).
- **Audit**: `Contribution.Updated`

---

### 6.6 Draw / Winner Selection

#### 6.6.1 Preview Eligible Members
- `GET /api/v1/pool-admin/draw/eligible?cycleId=<uuid>&month=<yyyy-mm-01>`
- **Eligibility rules (typical)**
  - Member is active and not suspended
  - Member has not already won in this cycle (unless multi-win enabled)
  - Member has paid required months up to month (configurable)
- Return list with `eligibilityFlags` per member to explain why excluded.

#### 6.6.2 Execute Draw (Random)
- `POST /api/v1/pool-admin/draw/execute`
- **Request**: `ExecuteDrawRequestDto`
  - `cycleId`
  - `month`
  - `method` = Random
  - `seed` (optional; for reproducibility in non-prod)
- **Rules**
  - Month must not already have a winner (409)
  - If eligible list empty => 422/400 business error
  - RNG must be cryptographically secure (`RandomNumberGenerator`).
- **Response**: `DrawResultDto` with `winnerMemberId`, `drawNumber`
- **Audit**: `Draw.Executed` with before/after.

#### 6.6.3 Set Winner (Manual)
- `POST /api/v1/pool-admin/draw/set-winner`
- **Request**: `SetWinnerRequestDto`
  - `cycleId`, `month`, `winnerMemberId`, `reason`
- **Rules**
  - Must still pass eligibility unless override policy is enabled (PoolSuperAdmin only)
- **Audit**: `Draw.WinnerSetManual`

#### 6.6.4 Re-Draw / Cancel Draw
- `POST /api/v1/pool-admin/draw/{drawId}/cancel`
- `POST /api/v1/pool-admin/draw/{drawId}/redraw`
- **Rules**
  - Redraw only if payout not released.
  - Must record cancellation reason.
- **Audit**: `Draw.Cancelled`, `Draw.Redrawn`

---

### 6.7 Payouts

#### 6.7.1 Get Payout Status (month)
- `GET /api/v1/pool-admin/payouts/status?cycleId=<uuid>&month=<yyyy-mm-01>`

#### 6.7.2 Mark Payout Released
- `POST /api/v1/pool-admin/payouts/release`
- **Request**: `ReleasePayoutRequestDto`
  - `cycleId`, `month`, `drawId`
  - `releasedOnUtc` (optional now)
  - `payoutRef` (optional)
  - `notes` (optional)
- **Rules**
  - Draw must exist and belong to cycle/month
  - Payout must be Pending => Released; otherwise 409
- **Audit**: `Payout.Released`

#### 6.7.3 Cancel Payout
- `POST /api/v1/pool-admin/payouts/{payoutId}/cancel`
- **Rules**
  - Only if Released reversal allowed (configurable) and requires approval workflow OR only Pending.
- **Audit**: `Payout.Cancelled`

---

### 6.8 Reports / Exports

#### 6.8.1 Monthly Collection Report
- `GET /api/v1/pool-admin/reports/monthly-collection?cycleId=<uuid>&month=<yyyy-mm-01>&format=<json|csv>`
- If CSV:
  - Return `text/csv` with filename; still include correlation headers.
- Columns:
  - Member, Amount, Status, PaidOn, Method, Ref, Notes

#### 6.8.2 Member Statement
- `GET /api/v1/pool-admin/reports/member-statement?memberId=<uuid>&cycleId=<uuid>&format=<json|pdf>`
- If PDF generation is not in scope, return JSON only and leave PDF to storage service later.

#### 6.8.3 Audit Log Export
- `GET /api/v1/pool-admin/audit?page=1&pageSize=50&from=<utc>&to=<utc>&action=<filter>`

---

### 6.9 Settings (Pool-level) — typically PoolSuperAdmin

#### 6.9.1 Get Pool Settings
- `GET /api/v1/pool-admin/settings`
- Settings examples:
  - `allowPartialPayments`
  - `allowEditAfterHours`
  - `eligibilityRule` options
  - `oneActiveCycleOnly`
  - `dueDayOfMonth`

#### 6.9.2 Update Pool Settings
- `PUT /api/v1/pool-admin/settings`
- **Auth**: PoolSuperAdmin+
- **Audit**: `Settings.Updated`

---

## 7. DTO Catalogue (C# contracts)

> Use **request DTOs** and **response DTOs** separately. Do not expose entities directly.

```csharp
public sealed record PagedResponseDto<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long TotalItems,
    int TotalPages
);
```

### 7.1 Members
```csharp
public sealed class CreateMemberRequestDto
{
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string RoleInPool { get; set; } = "Member"; // validate enum server-side
}

public sealed class MemberResponseDto
{
    public Guid MemberId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string RoleInPool { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsSuspended { get; set; }
    public DateTime JoinedOnUtc { get; set; }
}
```

### 7.2 Cycles
```csharp
public sealed class CreateCycleRequestDto
{
    public string CycleName { get; set; } = string.Empty;
    public DateOnly StartMonth { get; set; } // yyyy-mm-01 expected
    public decimal? ExpectedMonthlyShareAmount { get; set; }
    public string MembersSnapshotMode { get; set; } = "Live"; // Live|SnapshotOnActivation
}
```

### 7.3 Contributions
```csharp
public sealed class RecordContributionRequestDto
{
    public Guid CycleId { get; set; }
    public DateOnly Month { get; set; }
    public Guid MemberId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public DateTime? PaidOnUtc { get; set; }
    public string? ReferenceNo { get; set; }
    public string? Notes { get; set; }
}
```

### 7.4 Draw / Payout
```csharp
public sealed class ExecuteDrawRequestDto
{
    public Guid CycleId { get; set; }
    public DateOnly Month { get; set; }
    public string Method { get; set; } = "Random";
    public string? Seed { get; set; }
}

public sealed class DrawResultDto
{
    public Guid DrawId { get; set; }
    public Guid WinnerMemberId { get; set; }
    public int DrawNumber { get; set; }
    public DateTime DrawnOnUtc { get; set; }
}
```

---

## 8. State Machines (Business Rules)

### 8.1 Cycle State Machine
- Draft -> Active -> Closed -> Archived
- Invalid transitions => 409 Conflict

### 8.2 Contribution Status
- Pending -> Paid
- Pending -> Overdue (system)
- Paid -> Updated (audit, not status) OR Paid -> Reversed (if enabled)
- Partial is optional

### 8.3 Draw/Payout
- NoDraw -> Drawn(Pending) -> PayoutReleased
- Drawn(Pending) -> Cancelled (before release)
- Released -> Cancelled/Reversed (only if policy allows + approval)

---

## 9. Concurrency and Transactions

### 9.1 Concurrency Controls
- Use row-level locks or optimistic concurrency (version columns) on:
  - Contributions
  - Draw/Payout
  - Cycle status changes

### 9.2 Transaction Boundaries
- Activate cycle: one transaction for cycle + schedule generation
- Execute draw: one transaction (validate eligibility + insert draw + update month state)
- Bulk record: either per-item transaction or full batch transaction (choose explicitly)

---

## 10. Logging & Auditing Requirements

### 10.1 Audit Mandatory Actions
- Member: create/update/suspend/activate/deactivate
- Cycle: create/activate/close
- Contribution: record/update/waive/overdue override
- Draw: execute/set manual/cancel/redraw
- Payout: release/cancel

### 10.2 Audit Payload
- Store **minimal** before/after diffs where possible.
- Must never store full card/bank details (if any).
- Include: `CorrelationId`, `ActorUserId`, `TenantId`, `PoolId`.

---

## 11. Background Jobs (Optional but Recommended)

- Nightly Overdue evaluator (per pool timezone)
- Monthly report snapshot generator
- Reminder notifications (if mobile apps exist):
  - unpaid reminder
  - draw reminder
  - payout confirmation

---

## 12. API Task Breakdown Per Sprint (2-week sprints)

> This is a mid-level plan. Adjust sequence based on dependencies (auth/identity readiness).

### Sprint 1 — Foundation
- Project scaffolding (ASP.NET Core 8/9), DI, logging, correlation id middleware
- Generic response filter / helper
- Auth integration (JWT validation + claims access via UserContextService)
- Base error handling + validation pipeline

### Sprint 2 — Members
- Members CRUD + list/search/paging
- Suspend/activate endpoints + audits
- Unit tests for validation and scoping

### Sprint 3 — Cycles
- Cycle create/list/details
- Activate/close state machine + audits
- Data layer for cycle months + member snapshot (if enabled)

### Sprint 4 — Contributions v1
- Contribution list (by month) + summaries
- Record single payment + audits
- Overdue job (manual endpoint + scheduled worker placeholder)

### Sprint 5 — Contributions v2
- Bulk record with per-item results
- Edit/waive policies + audits
- Export monthly collection (JSON/CSV)

### Sprint 6 — Draw & Payout v1
- Eligible preview endpoint
- Execute draw (random) + audits
- Payout release endpoint + audits

### Sprint 7 — Draw & Payout v2
- Manual winner set + override policy
- Cancel/redraw flows + conflict handling
- Payout cancel (policy-based)

### Sprint 8 — Dashboard + Reports + Audit
- Dashboard metrics + recent activities
- Member statement endpoint
- Audit list/export + redaction rules
- Performance tuning + indexes review

### Sprint 9 — Hardening
- Rate limiting, idempotency, abuse protections
- Expanded integration tests
- Observability: metrics + tracing
- Documentation freeze + v1 release candidate

---

## 13. OpenAPI (Swagger) YAML — Starter (v1)

> This is a **starter** document intended to be expanded. It demonstrates:
> - security scheme
> - Generic response wrapper
> - key endpoints

```yaml
openapi: 3.0.3
info:
  title: Pool Admin API
  version: "1.0"
servers:
  - url: /
paths:
  /api/v1/pool-admin/members:
    get:
      tags: [Members]
      security: [{ bearerAuth: [] }]
      parameters:
        - in: query
          name: page
          schema: { type: integer, default: 1, minimum: 1 }
        - in: query
          name: pageSize
          schema: { type: integer, default: 25, minimum: 1, maximum: 200 }
        - in: query
          name: q
          schema: { type: string }
      responses:
        "200":
          description: OK
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/GenericPagedMemberResponse"
    post:
      tags: [Members]
      security: [{ bearerAuth: [] }]
      requestBody:
        required: true
        content:
          application/json:
            schema: { $ref: "#/components/schemas/CreateMemberRequest" }
      responses:
        "200":
          description: Created
          content:
            application/json:
              schema: { $ref: "#/components/schemas/GenericMemberResponse" }

components:
  securitySchemes:
    bearerAuth:
      type: http
      scheme: bearer
      bearerFormat: JWT

  schemas:
    GenericResponse:
      type: object
      properties:
        statusCode: { type: integer }
        message: { type: string }
        response: {}

    CreateMemberRequest:
      type: object
      required: [fullName, roleInPool]
      properties:
        fullName: { type: string, maxLength: 150 }
        phone: { type: string, maxLength: 30, nullable: true }
        email: { type: string, maxLength: 255, nullable: true }
        roleInPool: { type: string, example: "Member" }

    Member:
      type: object
      properties:
        memberId: { type: string, format: uuid }
        fullName: { type: string }
        phone: { type: string, nullable: true }
        email: { type: string, nullable: true }
        roleInPool: { type: string }
        isActive: { type: boolean }
        isSuspended: { type: boolean }
        joinedOnUtc: { type: string, format: date-time }

    PagedMember:
      type: object
      properties:
        items:
          type: array
          items: { $ref: "#/components/schemas/Member" }
        page: { type: integer }
        pageSize: { type: integer }
        totalItems: { type: integer, format: int64 }
        totalPages: { type: integer }

    GenericMemberResponse:
      allOf:
        - $ref: "#/components/schemas/GenericResponse"
        - type: object
          properties:
            response: { $ref: "#/components/schemas/Member" }

    GenericPagedMemberResponse:
      allOf:
        - $ref: "#/components/schemas/GenericResponse"
        - type: object
          properties:
            response: { $ref: "#/components/schemas/PagedMember" }
```

---

## 14. ASP.NET Core Skeletons (Controllers/Services)

> Minimal skeletons with required patterns (Generic response, route prefix, response types).

### 14.1 Members Controller
```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace YourProduct.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "PoolAdmin,PoolSuperAdmin,Support,Developer")]
public sealed class PoolAdminMembersController : ControllerBase
{
    private readonly IPoolAdminMembersService _service;

    public PoolAdminMembersController(IPoolAdminMembersService service) => _service = service;

    [HttpGet("pool-admin/members")]
    [ProducesResponseType(typeof(GenericReponseDto<PagedResponseDto<MemberResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(GenericReponseDto<dynamic>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(GenericReponseDto<dynamic>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 25, [FromQuery] string? q = null)
        => Ok(await _service.ListAsync(page, pageSize, q));

    [HttpPost("pool-admin/members")]
    [ProducesResponseType(typeof(GenericReponseDto<MemberResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(GenericReponseDto<dynamic>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(GenericReponseDto<dynamic>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create([FromBody] CreateMemberRequestDto req)
        => Ok(await _service.CreateAsync(req));
}
```

### 14.2 Service Interface
```csharp
public interface IPoolAdminMembersService
{
    Task<GenericReponseDto<PagedResponseDto<MemberResponseDto>>> ListAsync(int page, int pageSize, string? q);
    Task<GenericReponseDto<MemberResponseDto>> CreateAsync(CreateMemberRequestDto req);
}
```

---

## 15. Postman Collection (Starter)

> Import JSON to Postman. Replace variables accordingly.

```json
{
  "info": {
    "name": "Pool Admin API v1",
    "_postman_id": "00000000-0000-0000-0000-000000000001",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "variable": [
    { "key": "baseUrl", "value": "https://localhost:5001" },
    { "key": "token", "value": "PUT_JWT_HERE" }
  ],
  "item": [
    {
      "name": "Members - List",
      "request": {
        "method": "GET",
        "header": [
          { "key": "Authorization", "value": "Bearer {{token}}" },
          { "key": "X-Correlation-Id", "value": "{{$guid}}" }
        ],
        "url": {
          "raw": "{{baseUrl}}/api/v1/pool-admin/members?page=1&pageSize=25",
          "host": ["{{baseUrl}}"],
          "path": ["api","v1","pool-admin","members"],
          "query": [
            { "key": "page", "value": "1" },
            { "key": "pageSize", "value": "25" }
          ]
        }
      }
    },
    {
      "name": "Members - Create",
      "request": {
        "method": "POST",
        "header": [
          { "key": "Authorization", "value": "Bearer {{token}}" },
          { "key": "Content-Type", "value": "application/json" },
          { "key": "X-Correlation-Id", "value": "{{$guid}}" }
        ],
        "body": {
          "mode": "raw",
          "raw": "{\n  \"fullName\": \"Test Member\",\n  \"phone\": \"+923001234567\",\n  \"email\": \"test.member@example.com\",\n  \"roleInPool\": \"Member\"\n}"
        },
        "url": {
          "raw": "{{baseUrl}}/api/v1/pool-admin/members",
          "host": ["{{baseUrl}}"],
          "path": ["api","v1","pool-admin","members"]
        }
      }
    }
  ]
}
```

---

## 16. QA Acceptance Criteria (API)

### 16.1 Security
- PoolAdmin cannot access other pools’ data (verify by tampering IDs).
- Support cannot mutate states (403 on write endpoints).
- All endpoints require JWT unless explicitly allowed.

### 16.2 Data Integrity
- Draw cannot be executed twice for same month (409).
- Payout release cannot happen without draw (409/422).
- Suspended member cannot win (unless override policy by PoolSuperAdmin).

### 16.3 Auditing
- All privileged actions create audit log row with correct scoping and correlation id.

### 16.4 Pagination
- List endpoints enforce max pageSize.
- Total counts are correct.

---

## 17. Implementation Notes (Recommended)

- Use `UserContextService` (from JWT claims) to read `TenantId`, `PoolId`, `UserId`, `Roles`.
- Use explicit repository interfaces (no generic repo).
- Prefer Dapper (or EF) consistently; avoid mixing inside same module unless you have strict boundaries.
- Use DB unique constraints:
  - `Pool(TenantId, Code)` unique
  - `Membership(TenantId, PoolId, MemberId)` unique
- Add indexes:
  - Contributions: `(TenantId, PoolId, CycleId, Month, PaymentStatus)`
  - Draw: `(TenantId, PoolId, CycleId, Month)` unique
  - Audit: `(TenantId, PoolId, CreatedOnUtc)`

---

## 18. Out of Scope (Explicit)
- UI/Frontend behavior (covered in Designer SRS)
- Payment gateway integrations
- External SMS/Email provider implementation details (only hooks/events)
- Multi-currency support (unless specified)

---

## 19. Appendix — Endpoint Inventory (Quick Index)

**Dashboard**
- GET `/api/v1/pool-admin/dashboard/summary`
- GET `/api/v1/pool-admin/dashboard/activities`

**Members**
- GET `/api/v1/pool-admin/members`
- POST `/api/v1/pool-admin/members`
- PUT `/api/v1/pool-admin/members/{memberId}`
- POST `/api/v1/pool-admin/members/{memberId}/suspend`
- POST `/api/v1/pool-admin/members/{memberId}/unsuspend`
- POST `/api/v1/pool-admin/members/{memberId}/deactivate`
- POST `/api/v1/pool-admin/members/{memberId}/activate`

**Cycles**
- GET `/api/v1/pool-admin/cycles`
- POST `/api/v1/pool-admin/cycles`
- GET `/api/v1/pool-admin/cycles/{cycleId}`
- POST `/api/v1/pool-admin/cycles/{cycleId}/activate`
- POST `/api/v1/pool-admin/cycles/{cycleId}/close`

**Contributions**
- GET `/api/v1/pool-admin/contributions`
- POST `/api/v1/pool-admin/contributions/record`
- POST `/api/v1/pool-admin/contributions/bulk-record`
- PUT `/api/v1/pool-admin/contributions/{contributionId}`
- POST `/api/v1/pool-admin/contributions/{contributionId}/waive`
- POST `/api/v1/pool-admin/contributions/{contributionId}/mark-overdue`

**Draw**
- GET `/api/v1/pool-admin/draw/eligible`
- POST `/api/v1/pool-admin/draw/execute`
- POST `/api/v1/pool-admin/draw/set-winner`
- POST `/api/v1/pool-admin/draw/{drawId}/cancel`
- POST `/api/v1/pool-admin/draw/{drawId}/redraw`

**Payouts**
- GET `/api/v1/pool-admin/payouts/status`
- POST `/api/v1/pool-admin/payouts/release`
- POST `/api/v1/pool-admin/payouts/{payoutId}/cancel`

**Reports**
- GET `/api/v1/pool-admin/reports/monthly-collection`
- GET `/api/v1/pool-admin/reports/member-statement`
- GET `/api/v1/pool-admin/audit`
