# Designer SRS – Web Application (Pool Admin)
## Bachat Committee Management System

Document ID: BCS-DES-WEB-POOLADMIN-002  
Version: 2.0 (Detailed: Controls + Behavior + Workflows)  
Audience: UI/UX Designers (Web)  
Platform: Web Admin Portal (Desktop-first, Responsive)

---

## 0. PURPOSE

This is a designer-executable SRS for the **Pool Admin** role.
Pool Admin is scoped to assigned pool(s) only and does not have system-wide access.

---

## 1. ROLE & PERMISSIONS

### Pool Admin
**CAN**
- View pool dashboard
- Add/remove members (account + guest)
- View schedule (usually read-only)
- View cycles and instructions
- Monitor payments and confirmations
- Send reminders (if enabled by Super Admin)
- View disputes and add notes

**CANNOT**
- Create/delete pools
- Change global settings
- View audit logs
- Regenerate locked schedules (unless explicitly allowed)

---

## 2. GLOBAL UI (POOL-SCOPED)

### 2.1 Application Shell
- Header: Pool selector (only assigned pools), notifications, profile
- Sidebar:
  - Dashboard
  - Members
  - Schedule
  - Cycles
  - Instructions
  - Payments
  - Disputes
  - Notifications

**Behavior**
- All pages are auto-filtered to the selected pool.
- If user has only one pool, pool selector is hidden.

---

## 3. DASHBOARD (POOL OVERVIEW)

### 3.1 Page Skeleton
- Pool summary strip (status, current cycle, monthly amount, next due)
- Summary cards (members, expected, received, late)
- Unpaid list/table with quick actions
- Recent activity (within pool only)

### 3.2 Controls & Behavior
- Quick actions per unpaid member:
  - Send reminder (if allowed)
  - Open payment detail
- If reminders not allowed, show disabled button + tooltip “Not permitted”.

---

## 4. MEMBERS

### 4.1 Page Skeleton (Members)
- Add Member button
- Filters (type, reliability, status)
- Members table

### 4.2 Members Table Controls
- Actions: View, Remove
- Remove requires confirmation + reason
- Removing mid-cycle shows warning banner

### 4.3 Add Member Modal
- Account: search + select
- Guest: name required, phone optional

---

## 5. SCHEDULE

### 5.1 Page Skeleton
- Schedule table (cycle, month, receiver(s), split indicator, lock badge)
- Timeline toggle (optional)

### 5.2 Behavior
- Typically read-only. If regeneration allowed, requires reason + warning.
- Locked schedules show lock badge; editing disabled.

---

## 6. CYCLES

### 6.1 Page Skeleton
- Cycles table (cycle, month, receiver(s), expected, received, completion, actions)
- Actions: View cycle detail

### 6.2 Cycle Detail
- Receiver cards
- Payer status table (payer, amount, status, evidence indicator, confirmations)
- Open payment detail from row

---

## 7. INSTRUCTIONS

### 7.1 Page Skeleton
- Filters (cycle, status)
- Instructions table (payer, receiver, amount, due, status, evidence)

### 7.2 Behavior
- Read-only; Pool Admin cannot regenerate instructions (unless explicitly allowed).
- Row click opens payment detail.

---

## 8. PAYMENTS

### 8.1 Page Skeleton
- Filters (status, cycle)
- Payments table (payer, receiver, amount, status, evidence, confirmations)

### 8.2 Payment Detail (Pool Admin)
- View summary
- View evidence
- View confirmation history
- Allowed actions (configurable):
  - Send reminder
  - Add internal note
- Pool Admin cannot finalize disputes or override confirmations unless permitted.

---

## 9. DISPUTES

### 9.1 Page Skeleton
- Disputes table (payer, receiver, amount, status)
- Action: View dispute detail

### 9.2 Dispute Detail
- View evidence
- Add note/comment for Super Admin
- No “Resolve” button by default

---

## 10. NOTIFICATIONS

### 10.1 Page Skeleton
- Notifications list (reminder events, confirmations, admin messages)
- Filter by type/date (optional)

---

## 11. STATES & VISUAL LANGUAGE
- Payment status badges: Pending/Sent/Received/Late/Disputed
- Reliability badges: Green/Orange/Yellow/Red
- Locked/read-only states clearly communicated

---

## 12. EDGE CASES
- Guest member without account requires admin handling; show “Guest” label
- Member removed mid-cycle retains historical rows
- Reminder blocked by snooze/suppress shows badge on row
- Schedule changed by Super Admin shows “Updated” banner to Pool Admin

---

## 13. DESIGNER DELIVERABLES
- All pages above
- Dialogs (confirm + reason)
- Loading/empty/error states
- Component library

---

END OF WEB POOL ADMIN DESIGNER SRS
