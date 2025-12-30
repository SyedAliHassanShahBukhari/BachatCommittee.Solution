# Designer SRS – Web Application (Super Admin)
## Bachat Committee Management System

Document ID: BCS-DES-WEB-SUPERADMIN-005  
Version: 5.0 (Detailed: Controls + Behavior + Workflows)  
Audience: UI/UX Designers (Web)  
Platform: Web Admin Portal (Desktop-first, Responsive)

---

## 0. PURPOSE

This is a **designer-executable** SRS for the **Web Super Admin** role.
It defines:
- Page inventory
- Page skeletons (layout guidance)
- Controls/components on each page
- Expected workflows (happy path + key exceptions)
- States (loading/empty/error)
- Permission/locking behavior
- Confirmation dialogs and required fields (e.g., reasons)

---

## 1. ROLE & CONCEPTS

### 1.1 Role: Super Admin
System-wide authority to manage pools, members, schedules, cycles, instructions, payments, disputes, reminders, reliability, and audit logs.

### 1.2 Core Entities (UI terminology)
- **Pool**: Committee group (members + rules + schedule)
- **Cycle**: Monthly period instance (receiver(s), expected total, payment completion)
- **Instruction**: Who pays whom, how much, due date (per cycle)
- **Payment Evidence**: Screenshot/proof uploaded by payer
- **Confirmation**: Payer “Sent”, Receiver “Received”, Admin override
- **Dispute**: Conflict in payment confirmation/evidence
- **Reliability**: Member timeliness score (Green/Orange/Yellow/Red)

### 1.3 Status Vocabulary (must be consistent)
- Pool: Draft | Active | Closed
- Instruction/Payment: Pending | Sent | Received | Late | Disputed
- Schedule: Draft | Generated | Locked

---

## 2. GLOBAL UI (SHELL, NAV, COMPONENTS)

### 2.1 Application Shell
```
[ Top Header ]
- Logo (click → Dashboard)
- Pool Selector (All Pools / Specific Pool)
- Global Search (Pools, Members, Payers/Receivers)
- Notifications (badge)
- Profile Menu

[ Left Sidebar ]
- Dashboard
- Pools
- Members
- Schedule & Lucky Draw
- Cycles
- Instructions
- Payments
- Disputes
- Notifications & Reminders
- Reliability
- Audit Logs
- Settings

[ Main Content ]
- Page content
```
**Behavior**
- Pool selector filters all pool-scoped pages (Schedule/Cycles/Instructions/Payments/Disputes/Reminders/Reliability).
- Global search returns mixed results (Pools/Members/Payments). Selecting an item navigates to its detail page.

### 2.2 Global Components (design once, reuse everywhere)
- `PageHeader` (title, breadcrumbs, primary actions)
- `PrimaryButton`, `SecondaryButton`, `DangerButton`
- `FilterBar` (chips, dropdowns, date range)
- `DataTable` (sorting, pagination, export)
- `StatusBadge`
- `ReasonDialog` (mandatory textarea + confirm)
- `ConfirmDialog` (confirm/cancel)
- `Toast` (success/warning)
- `InlineErrorBanner`
- `EmptyStatePanel`
- `DrawerPanel` (side details)
- `EvidenceViewer` (image zoom/download)
- `AuditTimeline`

### 2.3 Global States
- Loading: skeleton rows/cards
- Empty: guided empty state with CTA
- Error: banner + retry button
- Read-only: disabled controls + tooltip “Locked/Closed”

---

## 3. DASHBOARD (SYSTEM OVERVIEW)

### 3.1 Page Skeleton
```
[ PageHeader: Dashboard ]
- Pool Selector (optional override)

[ Summary Cards Row ]
- Total Pools | Active Pools | Total Members | Outstanding Payments | Open Disputes

[ Charts Row ]
- Payment Completion (by status)
- Reliability Distribution

[ Pool Health Table ]
- Per pool: completion %, late count, disputes

[ Recent Activity Timeline ]
- Latest admin/member actions
```

### 3.2 Controls & Behavior
- Summary cards are clickable, deep-linking to pre-filtered pages.
- Charts support hover tooltips and click-to-filter the Pool Health Table.
- Recent Activity items open an Audit detail drawer (what changed, who, when, reason).

### 3.3 Dashboard Workflows
- **WF-DASH-01**: Click “Outstanding Payments” card → Payments page with Status = Pending/Late, All Pools.
- **WF-DASH-02**: Click a pool row “View” → Pool Detail Dashboard (see Section 4.4).

---

## 4. POOLS

### 4.1 Pools List Page

#### 4.1.1 Page Skeleton
```
[ PageHeader: Pools ]  (Primary CTA: Create Pool)
[ FilterBar ]
- Status (Draft/Active/Closed)
- Search by name
- Date created range (optional)

[ Pools DataTable ]
- Name | Status | Members | Current Cycle | Next Due | Health | Actions
```
#### 4.1.2 Controls
- Primary CTA: **Create Pool**
- Row actions: View | Edit | Close
- Bulk actions (optional): Export list

#### 4.1.3 Behavior Rules
- Closing a pool requires **ReasonDialog** and confirmation.
- Closed pool becomes read-only everywhere.
- Status badge color consistent across system.

#### 4.1.4 Workflows
- **WF-POOL-01 (Create)**: Create Pool → Save Draft → Add Members → Generate Schedule → Lock Schedule → Start Cycle.
- **WF-POOL-02 (Close)**: Close Pool → Reason required → Confirm → Pool status becomes Closed.

---

### 4.2 Create / Edit Pool Page

#### 4.2.1 Page Skeleton
```
[ PageHeader: Create Pool / Edit Pool ]  (Save, Cancel)
[ Tabs or Sections ]
1) Basic Info
2) Contribution & Rules
3) Schedule Setup
4) Reminder Rules
5) Permissions
6) Review (optional)
```
#### 4.2.2 Controls (per section)
**Basic Info**
- Pool Name (required)
- Currency (required)
- Description (optional)
- Pool Admin assignment (optional)

**Contribution & Rules**
- Monthly Amount (required)
- Payment Methods Allowed (multi-select: Bank/Raast/Easypaisa/JazzCash) (optional but recommended)
- Allow partial payouts (future toggle; default off)

**Schedule Setup**
- Start Month (required)
- Number of Cycles (required)
- Receiver selection mode: Lucky Draw | Manual (if allowed)

**Reminder Rules**
- Due Day (1–28)
- Reminder Day (e.g., 5th)
- Second Reminder Day (e.g., 10th)
- Grace Period (days)
- Escalation toggle (optional)

**Permissions**
- Receiver can Snooze reminders (toggle)
- Receiver can Suppress reminders (toggle)
- Allow schedule regeneration (toggle)
- Allow admin confirm on behalf (toggle; default on)

#### 4.2.3 Behavior
- Save validates required fields; show inline errors next to each field.
- Changing Schedule Setup after schedule is locked requires a warning dialog and may be disabled (recommended).

---

### 4.3 Pool Detail (Operational Dashboard)

#### 4.3.1 Page Skeleton
```
[ PageHeader: Pool Name ] (Actions: Edit Pool, Close Pool)
[ Pool Summary Strip ]
- Status | Current Cycle | Members | Monthly Amount | Next Due | Locked Schedule?

[ Cycle Progress Panel ]
- Progress bar (Received/Expected)
- Receiver(s) card(s)
- Split payout indicator

[ Unpaid Members Panel ]
- Table/list of pending/late payers with quick actions

[ Quick Links ]
- View Schedule | View Instructions | View Payments | View Disputes
```
#### 4.3.2 Controls
- Quick action per unpaid payer: Send Reminder | Snooze | Suppress | Open Payment Detail
- “Generate Instructions” CTA visible if schedule exists and cycle open

#### 4.3.3 Behavior
- If schedule not generated: show EmptyState with CTA “Run Lucky Draw / Set Schedule”.
- If locked schedule: show lock badge; regeneration may require reason and higher warning.

---

## 5. MEMBERS

### 5.1 Members List Page

#### 5.1.1 Page Skeleton
```
[ PageHeader: Members ] (CTA: Add Member)
[ FilterBar ]
- Pool (if All Pools view)
- Member Type (Account/Guest)
- Reliability (Green/Orange/Yellow/Red)
- Status (Active/Removed)
- Search

[ Members DataTable ]
- Name | Type | Pool(s) | Reliability | Late Ratio | Joined | Actions
```
#### 5.1.2 Controls
- Add Member (opens Add Member modal)
- Row actions: View | Remove | Move (optional) | Mark Inactive

#### 5.1.3 Add Member Modal (Controls)
- Tabs: Account Member | Guest Member
- Account: search + select
- Guest: Name (required), Phone (optional), Notes (optional)

#### 5.1.4 Behavior
- Removing member requires **ReasonDialog**.
- Removing mid-cycle triggers warning: “Member removal affects current cycle; instructions may need regeneration.”

---

## 6. SCHEDULE & LUCKY DRAW

### 6.1 Schedule Page

#### 6.1.1 Page Skeleton
```
[ PageHeader: Schedule & Lucky Draw ]
[ Action Bar ]
- Run Lucky Draw (Primary)
- Lock Schedule
- Regenerate Schedule (Danger/Secondary)
- Toggle: Table / Timeline view

[ Schedule Table ]
- Cycle | Month | Receiver(s) | Split? | Status | Actions
```
#### 6.1.2 Controls
- Run Lucky Draw button
- Lock Schedule toggle/button
- Regenerate button (requires reason)
- Row action: Configure Split (opens split form for that cycle)

#### 6.1.3 Lucky Draw Workflow (expected)
- **WF-SCH-01**: Click Run Lucky Draw → ConfirmDialog → animation/loader → show generated schedule preview → Save/Apply → optionally Lock.
- **WF-SCH-02**: Lock Schedule → ConfirmDialog → schedule becomes read-only.
- **WF-SCH-03**: Regenerate (if allowed) → ReasonDialog → confirm → new schedule generated and shown; previous schedule archived.

---

## 7. CYCLES & SPLIT PAYOUT

### 7.1 Cycles Page

#### 7.1.1 Page Skeleton
```
[ PageHeader: Cycles ]
[ FilterBar ]
- Pool
- Cycle status (Open/Closed)
- Month range

[ Cycles DataTable ]
- Cycle # | Month | Receiver(s) | Expected | Received | Completion% | Split | Actions
```
#### 7.1.2 Row Actions
- View Cycle Detail
- Configure Split (if allowed and cycle not closed)

### 7.2 Cycle Detail Page

#### 7.2.1 Page Skeleton
```
[ Cycle Summary ]
- Receiver(s) | Expected Total | Received Total | Completion | Due Dates

[ Receivers Panel ]
- Receiver cards with payment method details and shareable info

[ Payer Status Table ]
- Payer | Amount | Status | Evidence | Confirmations | Actions
```
#### 7.2.2 Controls
- Export cycle report
- Send reminders to unpaid (bulk)
- Open Payment Detail for a payer

### 7.3 Split Payout Form (per cycle)

#### 7.3.1 Controls
- Receiver multi-select (one or more)
- Percentage input per receiver
- Live “Total = 100%” indicator
- Visual split bar preview

#### 7.3.2 Behavior
- Save disabled until total = 100% and receivers selected.
- Changing split after any confirmations requires warning + reason (recommended).

---

## 8. INSTRUCTIONS (WHO PAYS WHOM)

### 8.1 Instructions List Page

#### 8.1.1 Page Skeleton
```
[ PageHeader: Instructions ] (CTA: Generate / Regenerate)
[ FilterBar ]
- Pool
- Cycle
- Status (Pending/Sent/Received/Late/Disputed)
- Receiver
- Search payer/receiver

[ Instructions DataTable ]
- Payer | Receiver | Amount | Due | Status | Evidence | Actions
```
#### 8.1.2 Controls
- Generate Instructions (primary)
- Regenerate Instructions (danger; requires reason)
- Row actions: View Instruction | Open Payment Detail

### 8.2 Generate Instructions Workflow (expected)
- **WF-INS-01**: Click Generate → opens “Preview & Validate” step:
  - Shows totals (expected vs generated)
  - Shows split distribution (if applicable)
  - Shows rounding notes (if any)
  - Confirm to finalize
- After finalization, instructions become immutable; changes require regeneration with reason.

---

## 9. PAYMENTS & CONFIRMATIONS

### 9.1 Payments List Page

#### 9.1.1 Page Skeleton
```
[ PageHeader: Payments ]
[ FilterBar ]
- Pool
- Cycle
- Status
- Receiver
- Payer
- Date range

[ Payments DataTable ]
- Payer | Receiver | Amount | Due | Status | Evidence | Confirmations | Actions
```
#### 9.1.2 Controls
- Bulk: Send reminders (to filtered unpaid)
- Export payments

### 9.2 Payment Detail Page (CRITICAL)

#### 9.2.1 Page Skeleton
```
[ Payment Summary Strip ]
- Amount | Payer | Receiver | Cycle | Due | Status

[ Evidence Viewer ]
- Image preview, zoom, download, timestamp

[ Confirmation History Timeline ]
- Payer marked Sent (time)
- Receiver marked Received (time)
- Admin actions (time + reason)

[ Admin Action Panel ]
- Confirm on behalf of Receiver
- Confirm on behalf of Payer (rare; optional)
- Mark as Disputed
- Add Note
```
#### 9.2.2 Required dialogs
- Admin Confirm → ReasonDialog (required)
- Mark Disputed → ReasonDialog (required) + optional “Request more evidence” checkbox

#### 9.2.3 Behavior
- If payer Sent but no evidence → show warning and allow admin to request evidence.
- Conflicting confirmations automatically set status = Disputed and surface in Disputes page.

---

## 10. DISPUTES

### 10.1 Disputes List Page

#### 10.1.1 Page Skeleton
```
[ PageHeader: Disputes ]
[ FilterBar ]
- Pool
- Cycle
- Status (Open/Resolved)
- Search

[ Disputes DataTable ]
- Payer | Receiver | Amount | Reason | Created | Status | Actions
```
### 10.2 Dispute Detail & Resolution

#### 10.2.1 Page Skeleton
```
[ Dispute Summary ]
- Parties, amount, cycle, current status

[ Evidence Comparison ]
- Payer evidence
- Receiver notes (if any)
- Admin notes

[ Resolution Panel ]
- Select final outcome:
  - Mark as Received
  - Mark as Not Received (revert)
  - Keep Disputed (needs external resolution)
- Resolution note (required)
- Save Resolution
```
#### 10.2.2 Behavior
- Resolving dispute writes an audit event and notifies both parties.
- Resolution note is mandatory.

---

## 11. NOTIFICATIONS & REMINDERS

### 11.1 Reminders Page

#### 11.1.1 Page Skeleton
```
[ PageHeader: Notifications & Reminders ]
[ Reminder Rules Summary ]
- Due day, reminder days, grace period

[ Reminder Queue Table ]
- Member | Cycle | Due | Status | Next Reminder Date | Actions

[ Broadcast Panel ]
- Send announcement to pool members (optional)
```
#### 11.1.2 Controls
- Send reminder now (per member)
- Snooze (admin)
- Suppress (admin)
- View reminder history

#### 11.1.3 Behavior
- Reminders are triggered automatically (per configured days) and logged.
- Snooze/Suppress requires reason; displays badge on member entry.

---

## 12. RELIABILITY

### 12.1 Reliability Page

#### 12.1.1 Page Skeleton
```
[ PageHeader: Reliability ]
[ FilterBar ]
- Pool
- Badge color
- Late ratio range
- Search member

[ Reliability DataTable ]
- Member | Badge | Late Ratio | On-time count | Late count | Notes | Actions
```
#### 12.1.2 Behavior
- Clicking member opens a drawer with per-cycle history and late pattern.

---

## 13. AUDIT LOGS

### 13.1 Audit Logs Page

#### 13.1.1 Page Skeleton
```
[ PageHeader: Audit Logs ]
[ FilterBar ]
- Pool
- Entity type (Pool/Member/Schedule/Instruction/Payment/Dispute)
- User
- Date range

[ AuditTimeline ]
- Who | Action | Timestamp | Reason | View Details
```
#### 13.1.2 Behavior
- “View Details” opens drawer showing before/after snapshot.

---

## 14. STATES & VISUAL LANGUAGE

### 14.1 Status Badges
- Pending (Gray)
- Sent (Blue)
- Received (Green)
- Late (Orange)
- Disputed (Red)

### 14.2 Reliability Badges
- Green (on-time)
- Orange (slight delays)
- Yellow (frequent delays)
- Red (high risk)

Each badge must provide tooltip explanation.

---

## 15. KEY EDGE CASES (MUST BE DESIGNED)

- Guest member included in cycles (no app access); admin confirms on behalf
- Schedule regenerated after some confirmations: show warning + lock old data in history
- Split payout changed mid-cycle: require reason and show audit trail
- Pool closed with pending payments: read-only, still visible in reports
- Conflicting confirmations: auto-create dispute and surface prominently
- Evidence upload missing or invalid: inline warnings, request re-upload

---

## 16. DESIGNER DELIVERABLE CHECKLIST

Designer must deliver:
- All pages above
- All dialogs (confirm/reason/snooze/suppress)
- Loading/empty/error states for each table
- Component library (badges, tables, dialogs, evidence viewer, drawers)
- Responsive behavior (min: desktop + tablet)

---

END OF WEB SUPER ADMIN DESIGNER SRS
