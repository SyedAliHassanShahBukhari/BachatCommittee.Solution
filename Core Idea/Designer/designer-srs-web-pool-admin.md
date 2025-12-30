# Designer SRS – Web Application (Pool Admin)
## Bachat Committee Management System

Document ID: BCS-DES-WEB-POOLADMIN-003  
Version: 3.0 (FULL – Controls, Behavior, Workflows, Edge Cases)  
Audience: UI/UX Designers (Web)  
Platform: Web Admin Portal (Desktop-first, Responsive)

---

## 0. PURPOSE & SCOPE

This document is a **designer-executable SRS** for the **Web Pool Admin** role.

It defines:
- All pages accessible to Pool Admin
- Page skeletons (layout guidance)
- UI controls and components
- Expected workflows (happy paths + exceptions)
- Permission and locking behavior
- Edge cases and visual states

A designer must be able to produce **complete wireframes and UI designs**
without further requirement-gathering sessions.

---

## 1. ROLE DEFINITION

### Pool Admin

A Pool Admin manages **one or more assigned pools only** and operates day-to-day committee activities.

### 1.1 Permissions

**Pool Admin CAN**
- View pool dashboard
- Add / remove members (account + guest)
- View schedules and cycles
- View and monitor payment instructions
- View payment confirmations and evidence
- Send reminders (if enabled by Super Admin)
- Add notes on disputes

**Pool Admin CANNOT**
- Create or delete pools
- Access system-wide dashboards
- Modify global pool rules
- Regenerate locked schedules (unless explicitly allowed)
- Resolve disputes (final decision)
- Access audit logs

---

## 2. GLOBAL LAYOUT & NAVIGATION

### 2.1 Application Shell

```
[ Top Header ]
- Logo (click → Pool Dashboard)
- Pool Selector (only assigned pools)
- Notifications Icon (badge)
- Profile Menu

[ Left Sidebar ]
- Dashboard
- Members
- Schedule
- Cycles
- Instructions
- Payments
- Disputes
- Notifications

[ Main Content Area ]
- Page-specific content
```

### 2.2 Global Behavior
- If user has only one pool → pool selector hidden
- All pages auto-filter to selected pool
- Locked pools show a lock badge globally

---

## 3. POOL DASHBOARD

### 3.1 Purpose
Provide an **operational snapshot** of the selected pool.

---

### 3.2 Page Skeleton

```
[ PageHeader ]
- Pool Name
- Pool Status Badge
- Current Cycle

[ Summary Cards ]
- Total Members
- Expected Payments (Current Cycle)
- Received Payments
- Late Payments

[ Cycle Progress Panel ]
- Progress bar (Received / Expected)
- Receiver(s) cards
- Split payout indicator

[ Unpaid Members Panel ]
- Table/List of Pending & Late payers

[ Recent Activity Panel ]
- Payment confirmations
- Reminder actions
```

### 3.3 Controls & Behavior
- Clicking a summary card deep-links to filtered list (e.g., Late → Payments)
- If no active cycle → show empty state with “Waiting for next cycle” message
- Split payout indicator shows tooltip explaining distribution

### 3.4 Edge Cases
- Pool closed → dashboard read-only banner
- Cycle exists but instructions not generated → warning banner

---

## 4. MEMBERS MANAGEMENT

### 4.1 Page Skeleton (Members)

```
[ PageHeader: Members ] (CTA: Add Member)
[ FilterBar ]
- Member Type (Account / Guest)
- Reliability Badge
- Status (Active / Removed)
- Search

[ Members DataTable ]
- Name | Type | Reliability | Status | Joined | Actions
```

### 4.2 Members Table – Controls
- View Member (drawer)
- Remove Member (confirmation + reason required)

### 4.3 Add Member Modal

**Tabs**
- Account Member
- Guest Member

**Account Member Controls**
- Search (autocomplete)
- Select user
- Add

**Guest Member Controls**
- Name (required)
- Phone (optional)
- Notes (optional)

### 4.4 Behavior Rules
- Removing a member mid-cycle triggers warning:
  “Member removal may affect current cycle instructions.”
- Removed members remain visible in historical views
- Guest members are labeled clearly in all tables

---

## 5. SCHEDULE (POOL VIEW)

### 5.1 Page Skeleton

```
[ PageHeader: Schedule ]
[ Schedule Table ]
- Cycle | Month | Receiver(s) | Split | Status | Lock Badge
```

### 5.2 Controls & Behavior
- Schedule is read-only by default
- If regeneration allowed:
  - Regenerate button visible
  - Requires reason dialog
- Locked schedules cannot be edited

### 5.3 Edge Cases
- Schedule updated by Super Admin → show “Schedule Updated” info banner
- No schedule exists → empty state with guidance text

---

## 6. CYCLES

### 6.1 Page Skeleton (Cycles)

```
[ PageHeader: Cycles ]
[ FilterBar ]
- Status (Open / Closed)
- Month range

[ Cycles DataTable ]
- Cycle # | Month | Receiver(s) | Expected | Received | Completion | Actions
```

### 6.2 Cycle Detail Page

```
[ Cycle Summary ]
- Cycle number
- Month
- Receiver(s)
- Expected / Received totals

[ Receiver Cards ]
- Receiver name
- Share %
- Payment details

[ Payer Status Table ]
- Payer | Amount | Status | Evidence | Confirmations | Actions
```

### 6.3 Behavior
- Clicking payer opens Payment Detail view
- Bulk reminder available for unpaid payers (if allowed)

---

## 7. INSTRUCTIONS (WHO PAYS WHOM)

### 7.1 Page Skeleton

```
[ PageHeader: Instructions ]
[ FilterBar ]
- Cycle
- Status
- Search

[ Instructions DataTable ]
- Payer | Receiver | Amount | Due Date | Status | Evidence
```

### 7.2 Behavior
- Instructions are read-only for Pool Admin
- Status updates in real time as confirmations occur
- Evidence icon opens Payment Detail

---

## 8. PAYMENTS

### 8.1 Page Skeleton

```
[ PageHeader: Payments ]
[ FilterBar ]
- Status
- Cycle
- Search payer/receiver

[ Payments DataTable ]
- Payer | Receiver | Amount | Due | Status | Evidence | Actions
```

### 8.2 Payment Detail Page

```
[ Payment Summary ]
- Amount | Payer | Receiver | Cycle | Due | Status

[ Evidence Viewer ]
- Screenshot preview (zoom/download)

[ Confirmation History ]
- Payer Sent
- Receiver Received
- Admin Overrides (if any)

[ Pool Admin Actions ]
- Send Reminder (if allowed)
- Add Internal Note
```

### 8.3 Behavior
- Pool Admin cannot override confirmations by default
- Notes are visible to Super Admin
- If evidence missing → show warning banner

---

## 9. DISPUTES

### 9.1 Page Skeleton

```
[ PageHeader: Disputes ]
[ FilterBar ]
- Status (Open / Resolved)
- Cycle

[ Disputes DataTable ]
- Payer | Receiver | Amount | Reason | Status | Actions
```

### 9.2 Dispute Detail Page

```
[ Dispute Summary ]
- Parties
- Amount
- Cycle
- Current Status

[ Evidence Section ]
- Payer evidence
- Receiver notes (if any)

[ Pool Admin Notes ]
- Comment box (for Super Admin)
```

### 9.3 Behavior
- Pool Admin cannot resolve disputes
- Notes trigger notification to Super Admin

---

## 10. NOTIFICATIONS

### 10.1 Page Skeleton

```
[ PageHeader: Notifications ]
[ Notifications List ]
- Reminder events
- Confirmation updates
- Admin messages
```

### 10.2 Behavior
- Notifications are read-only
- Clicking notification navigates to related entity

---

## 11. STATUS & VISUAL LANGUAGE

### Payment Status Badges
- Pending (Gray)
- Sent (Blue)
- Received (Green)
- Late (Orange)
- Disputed (Red)

### Reliability Badges
- Green
- Orange
- Yellow
- Red

Tooltips required for all badges.

---

## 12. GLOBAL EDGE CASES (MUST BE DESIGNED)

- Guest member has no app access → admin confirms on behalf
- Member removed mid-cycle retains history
- Reminder blocked due to snooze/suppress
- Schedule regenerated by Super Admin mid-cycle
- Pool closed with pending payments
- Evidence uploaded after dispute opened

---

## 13. DESIGNER DELIVERABLE CHECKLIST

Designer must deliver:
- All pages described above
- All dialogs (confirm, warning, reason)
- Loading, empty, and error states
- Responsive layouts
- Component library

---

END OF WEB POOL ADMIN DESIGNER SRS
