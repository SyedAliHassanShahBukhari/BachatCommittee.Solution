
# Software Requirements Specification (SRS)
## Bachat Committee Management System (Web + Mobile)

**Document ID:** BCS-SRS-001  
**Version:** 1.0  
**Status:** Draft (Ready for Build)  
**Date:** 2025-12-29  
**Primary Owner:** Super Admin / Committee Manager  
**Prepared For:** Development Team (Backend, Mobile, Web), QA, DevOps



## 0. Executive Summary
This system digitizes and automates a **Bachat (Savings) Committee** workflow. A **Super Admin** creates one or more committee pools, adds **account members** and **guest (non-account) members**, runs a **live (real-time) lucky draw** to define payout order, and the system automatically generates **payer → receiver payment instructions** each cycle (month). The system supports **split payouts** (e.g., 70% to one receiver, 30% to another) by computing exactly **who pays whom and how much**. Members can upload **payment proofs (screenshots)** and confirm payment as **sent**; receivers confirm **received**; admin can confirm **on behalf of guest members**. The system provides **group chat per pool**, **private chat**, configurable **reminders** (5th/10th), reminder **snooze/suppress** by the current month receiver(s), and a **member punctuality indicator** (Green/Orange/Yellow/Red) based on late payment history.

This SRS is structured to be implementable without additional requirement-gathering sessions, following the discipline of well-formed requirements and SRS structuring guidance (ISO/IEC/IEEE 29148).  
References:
- ISO/IEC/IEEE 29148 summary and standard listing: https://www.iso.org/standard/72089.html and IEEE standard page https://standards.ieee.org/standard/29148-2018.html :contentReference[oaicite:0]{index=0}



## 1. Purpose and Scope

### 1.1 Purpose
Define **complete functional and non-functional requirements** for building the Bachat Committee Management System, including:
- Admin governance
- Member management (account + guest)
- Real-time lucky draw and scheduling
- Payment instruction generation (including split payouts)
- Confirmations and evidence
- Chats
- Notifications and exceptions
- Reliability scoring
- Auditability, security, and operational requirements

### 1.2 Scope
**In Scope (Phase 1 / MVP+):**
- External payment tracking (not payment processing)
- Multiple pools
- Mixed members: account holders and guest entries
- Live lucky draw (real-time broadcast)
- Automatic monthly instruction generation
- Evidence uploads (screenshots)
- Confirmation workflow
- Dispute workflow
- Reminders + snooze/suppress
- Receiver-of-the-month dynamic privileges
- Reliability scoring badges

**Out of Scope (Explicitly Not Included in Phase 1):**
- In-app wallet / payment gateway settlement
- Automated bank reconciliation
- KYC/identity verification
- Legal contract management
(These can be Phase 2+.)



## 2. Definitions and Terminology
- **Pool / Committee:** A savings group.
- **Cycle:** One period (normally one month) in which contributions are made and payout occurs.
- **Payer:** Member obligated to pay contribution for the cycle.
- **Receiver:** Member(s) receiving the payout for the cycle.
- **Split payout:** A cycle payout distributed to multiple receivers with percentages that sum to 100%.
- **Instruction:** A computed record: “Payer X pays Receiver Y amount Z by due date D.”
- **Account Member:** Registered user who logs in (mobile/web).
- **Guest Member:** Non-account participant, managed by admin (record-only).
- **Super Admin:** System-level admin who can create/manage pools and resolve disputes.
- **Receiver-of-the-Month Privileges:** Time-bound permissions for current cycle receiver(s) (auto enabled/disabled).
- **Evidence:** Screenshot/receipt uploaded for a payment instruction.
- **Reminder exception:** Snooze/suppress rule applied to a payer/instruction for a cycle.



## 3. Stakeholders and User Roles

### 3.1 Stakeholders
- **Super Admin / Committee Organizer**
- **Account Members (Participants)**
- **Receivers (subset of members, changes monthly)**
- **Support / Operations (optional)**
- **Development Team / QA / DevOps**

### 3.2 Roles (Authorization Model)
- **SuperAdmin**
  - Full control over all pools and global settings.
- **PoolAdmin** (optional; can be same as SuperAdmin initially)
  - Admin control limited to assigned pools.
- **Member**
  - Participates, views instructions, confirms sent/received, chat.
- **ReceiverManager (Dynamic Role)**
  - Enabled automatically for current cycle receiver(s).
  - Can confirm received, manage reminder exceptions (if enabled by policy).

> Security implementation must follow a recognized verification baseline such as OWASP ASVS for auth/session/access control and secure development requirements.  
OWASP ASVS (official): https://owasp.org/www-project-application-security-verification-standard/ :contentReference[oaicite:1]{index=1}



## 4. Product Overview and Platforms

### 4.1 Target Platforms
- **Backend:** ASP.NET Core Web API (recommended)  
  ASP.NET Core overview: https://learn.microsoft.com/en-us/aspnet/core/overview :contentReference[oaicite:2]{index=2}
- **Realtime:** SignalR (recommended)  
  SignalR introduction: https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction :contentReference[oaicite:3]{index=3}
- **Mobile App:** Flutter (Android/iOS)  
  Flutter docs: https://docs.flutter.dev/ :contentReference[oaicite:4]{index=4}
- **Data Access:** Dapper  
  Dapper GitHub: https://github.com/DapperLib/Dapper :contentReference[oaicite:5]{index=5}
- **API Documentation:** OpenAPI + Swagger UI  
  OpenAPI Spec: https://spec.openapis.org/ and Swagger OAS 3.1 docs: https://swagger.io/specification/ :contentReference[oaicite:6]{index=6}

### 4.2 High-Level Architecture
- Mobile App (Flutter) and Web Portal (Admin + Member web) call REST APIs.
- SignalR hub pushes:
  - live lucky draw updates
  - live payment status changes
  - live chat messages
- Database stores all pools, schedule, instructions, confirmations, evidence metadata, chats, notifications, audit logs.



## 5. Business Rules (Complete)

### 5.1 Pool Rules
BR-1: A pool has a **start month**, **number of cycles**, and **contribution model**.  
BR-2: Default cycle frequency is monthly.  
BR-3: Pool must support **mixed membership**: account + guests.  
BR-4: A pool may be **Draft**, **Active**, or **Closed**.  
BR-5: Once active, schedule and critical rules must be change-controlled (audit + reason).  

### 5.2 Lucky Draw Rules
BR-6: Lucky draw assigns **default receiver order** for each cycle.  
BR-7: Lucky draw must be **broadcast live** to currently connected pool members.  
BR-8: Draw results must be **persisted** with an audit trail.  
BR-9: If schedule is regenerated, previous schedule must remain retrievable in audit history.

SignalR is designed for server-to-client real-time push, suitable for live draw broadcasting. :contentReference[oaicite:7]{index=7}

### 5.3 Payment Rules
BR-10: Payments occur **outside** the system (bank, wallet, etc.); system tracks status only.  
BR-11: Each cycle produces a set of **Payment Instructions**.  
BR-12: A payer can have multiple instructions in a cycle (if split payout requires it).  
BR-13: A receiver can receive from multiple payers.  
BR-14: Due date defaults to pool due day; reminders follow configured schedule.

### 5.4 Split Payout Rules
BR-15: A cycle can have **1..N receivers** with percentages totaling 100%.  
BR-16: The instruction generator must compute payer → receiver mapping so totals match target percentages.  
BR-17: Rounding must be deterministic and logged.

**Example (Discrete Mapping):**
- Total payers: 10, contribution per payer: 10,000 PKR, total pot: 100,000 PKR  
- Receivers: A = 70% (70,000), B = 30% (30,000)  
- System maps: 7 payers → A, 3 payers → B (each pays 10,000)

### 5.5 Guest Member Rules
BR-18: Guest members cannot login, chat, upload evidence, or self-confirm.  
BR-19: Admin (and optionally ReceiverManager) can record confirmations on their behalf.  
BR-20: All “on behalf of guest” actions must be clearly labeled and auditable.

### 5.6 Confirmation Rules
BR-21: A payment instruction can be confirmed by:
- payer (sent)
- receiver (received)
- admin (override or on behalf)
BR-22: Status transitions must be controlled and consistent:
- Pending → PayerConfirmed → ReceiverConfirmed (or AdminConfirmed)
- Any state → Disputed (if conflict)
- Disputed → Resolved (Admin action)
BR-23: Receivers can confirm partial receipts (tick per payer).

### 5.7 Reminder and Exception Rules
BR-24: Reminders are sent only to unpaid members/instructions.  
BR-25: Receiver-of-the-month can snooze/suppress reminders for specific payers for that cycle.  
BR-26: Those privileges expire automatically after cycle ends.

### 5.8 Reliability / Punctuality Rules
BR-27: System calculates late payment ratio per member.
- Green: always/mostly on time
- Orange: slightly late occasionally
- Yellow: ~30% late (e.g., 3 out of 10 late)
- Red: ≥50% late
BR-28: Thresholds must be configurable at pool or global level.
BR-29: Indicators must be visible to admin in pool member lists and dashboards.



## 6. Functional Requirements (Detailed)

### 6.1 Authentication and User Management
FR-AUTH-1: Account members can register and login.  
FR-AUTH-2: JWT authentication is used for API access (mobile/web).  
FR-AUTH-3: Role-based access control enforced on every endpoint.  
FR-AUTH-4: Session/token security should follow OWASP ASVS guidance. :contentReference[oaicite:8]{index=8}

### 6.2 Pool Management
FR-POOL-1: Super Admin can create a pool with:
- name
- currency
- start month
- cycle count
- contribution model (Fixed in Phase 1)
- fixed contribution amount
- due day of month (e.g., 5)
- reminder days (e.g., 5 and 10)
- grace period (days)
- policy flags (receiver can snooze? receiver can suppress? allow regen schedule?)

FR-POOL-2: Super Admin can edit pool settings with audit reason.  
FR-POOL-3: Super Admin can activate pool (locks some draft-only edits).  
FR-POOL-4: Super Admin can close pool (read-only thereafter).  
FR-POOL-5: System maintains pool audit history of changes.

### 6.3 Member Management (Account + Guest)
FR-MEM-1: Admin adds account members to pool.  
FR-MEM-2: Admin adds guest members with at least display name.  
FR-MEM-3: Admin can edit guest details (name, phone, notes).  
FR-MEM-4: Admin can deactivate/exit a member with reason.  
FR-MEM-5: System prevents duplicate account membership in same pool.  
FR-MEM-6: System keeps display-name snapshot to preserve history.

### 6.4 Lucky Draw and Scheduling
FR-DRAW-1: Admin can run a lucky draw for a pool.  
FR-DRAW-2: Draw events are broadcast in real time to pool group via SignalR. :contentReference[oaicite:9]{index=9}  
FR-DRAW-3: Draw output generates a payout schedule for all cycles.  
FR-DRAW-4: Admin can lock schedule.  
FR-DRAW-5: Regenerate schedule requires reason and creates new draw event referencing the previous.  
FR-DRAW-6: Schedule view is available to members (read-only) and admin (read/write controls).  
FR-DRAW-7: System supports “manual schedule override” only if enabled by pool policy; all changes are audited.

### 6.5 Cycle Payout Configuration (Split Payout)
FR-PAYOUT-1: For each cycle, system determines receiver(s) as:
- default receiver from schedule OR
- overridden by split payout config

FR-PAYOUT-2: Admin can configure split payout for a cycle:
- receiver list with percentage per receiver
- validation: sum = 100
- store notes and audit

FR-PAYOUT-3: Receiver-of-the-month privileges apply to all configured receivers for that cycle.

### 6.6 Payment Instruction Generation (Who Pays Whom)
FR-INST-1: Admin can generate payment instructions for a cycle.  
FR-INST-2: Generation uses:
- pool members eligible as payers (active members, excluding exited)
- contribution amount per payer (fixed in phase 1)
- receiver(s) and percentages (100% or split)

FR-INST-3: Instruction generator outputs a set of records:
- payer
- receiver
- amount
- due date
- receiver payment method details (if available)
- current status = Pending

FR-INST-4: Instruction generation strategies (Phase 1 supports at least one):
- **Discrete Payer Mapping (recommended):** assign whole payers to receivers until target totals met.
- **Proportional Split per Payer (optional):** each payer pays multiple receivers in same cycle (more complex UX).

FR-INST-5: For Discrete Mapping:
- System selects which payers pay which receiver.
- Selection must be deterministic given the same input (e.g., sort payers by join date or member ID and assign in order) to avoid disputes.

FR-INST-6: Rounding rules:
- all amounts are in whole currency units (PKR)
- any remainder distributed per configured priority
- log rounding adjustment details

FR-INST-7: Members can view:
- “My instructions for this cycle”
- “My outgoing payment obligations”
- “My incoming expected payments” (if receiver)

FR-INST-8: Admin can view:
- all instructions
- per receiver expected vs received counts/amounts
- per payer unpaid list

### 6.7 Payment Evidence (Screenshots/Receipts)
FR-EVID-1: Payer (account) can upload evidence for an instruction.  
FR-EVID-2: Evidence is stored securely (file storage) with metadata:
- uploader, time, linked instruction, hash (optional)
FR-EVID-3: Admin can upload evidence on behalf (optional).  
FR-EVID-4: Evidence viewable by:
- payer
- receiver
- admin
- not visible to unrelated members

### 6.8 Payment Confirmation Workflow
FR-CONF-1: Payer can mark instruction as “Sent” with optional evidence.  
FR-CONF-2: Receiver can mark instruction as “Received” (tick per payer).  
FR-CONF-3: Admin can confirm on behalf of guest payer/receiver.  
FR-CONF-4: Confirmation events must be recorded with:
- actor type (payer/receiver/admin)
- timestamp
- notes
- “on behalf of guest” flag where applicable

FR-CONF-5: Status model (minimum):
- Pending
- PayerConfirmed
- ReceiverConfirmed
- AdminConfirmed
- Disputed
- Resolved
- Cancelled (rare; admin only)

FR-CONF-6: Partial progress is supported:
- receiver sees list of expected payers and can tick as received.
- admin dashboard shows x/y received.

### 6.9 Disputes and Resolution
FR-DISP-1: Payer or receiver can flag an instruction as disputed with reason.  
FR-DISP-2: Admin can resolve dispute with:
- resolution notes
- optional evidence reference
- final status set to ReceiverConfirmed or AdminConfirmed or Cancelled
FR-DISP-3: All dispute actions are auditable.

### 6.10 Notifications and Reminders
FR-NOTIF-1: System sends reminder notifications to unpaid payers:
- on due day (e.g., 5th)
- on second reminder day (e.g., 10th)
FR-NOTIF-2: Reminder schedule must be configurable per pool.  
FR-NOTIF-3: Notifications channels (Phase 1):
- in-app push (mobile)
- in-app notification center
(Optional Phase 2: SMS/WhatsApp/email gateways.)

FR-NOTIF-4: Reminder exceptions:
- snooze until date
- suppress for cycle
FR-NOTIF-5: Receiver-of-the-month and/or admin can create reminder exceptions (policy-based).  
FR-NOTIF-6: Once cycle changes, receiver privileges auto expire and exceptions remain historically recorded.

### 6.11 Chat (Group and Private)
FR-CHAT-1: Each pool has a group chat thread for account users in that pool.  
FR-CHAT-2: Private chat supported:
- member ↔ admin
- member ↔ receiver-of-the-month (optional policy)
FR-CHAT-3: Chat supports:
- text messages
- optional attachment linking to evidence (or direct file upload in Phase 2)
FR-CHAT-4: A chat message can reference a payment instruction (link).  
FR-CHAT-5: Real-time chat delivery via SignalR. :contentReference[oaicite:10]{index=10}  
FR-CHAT-6: Guests do not chat.

### 6.12 Receiver-of-the-Month Dynamic Privileges
FR-PRIV-1: System automatically grants ReceiverManager privileges to current cycle receiver(s).  
FR-PRIV-2: Privileges automatically revoked at cycle end and granted to next cycle receiver(s).  
FR-PRIV-3: Privileges include:
- confirm received
- create reminder exceptions (if allowed by pool settings)

### 6.13 Reliability / Punctuality Scoring
FR-REL-1: System computes late ratio per member across the pool.  
FR-REL-2: Thresholds configurable:
- Grace period (days)
- Orange/yellow/red thresholds (percentages)
FR-REL-3: Badge rules (default):
- Green: late ratio < 10%
- Orange: 10%–29%
- Yellow: 30%–49% (matches “3 out of 10” concept)
- Red: ≥ 50%
FR-REL-4: Display reliability badges in:
- pool member list
- member profile
- admin dashboard filters
FR-REL-5: Reliability recalculated:
- after each instruction final confirmation OR
- nightly batch job OR both



## 7. User Experience Requirements (UI/UX)

### 7.1 Admin Portal (Web)
Admin must have:
- Pool list + status
- Create/edit pool
- Member management (account + guest)
- Lucky draw control panel (start, lock, regenerate)
- Schedule viewer (by month/cycle)
- Cycle payout config (split)
- Instruction generation + preview + finalize
- Cycle dashboard:
  - receiver expected vs received
  - unpaid payers list
  - disputed instructions
- Confirmation management and override
- Reminder management:
  - run reminders (manual)
  - view reminder logs
  - manage snooze/suppress
- Reliability dashboard:
  - filter red/yellow members
- Chat moderation (optional)
- Audit log viewer

### 7.2 Member Mobile App (Flutter)
Member must have:
- “My Pools” list
- Pool details:
  - schedule timeline
  - my next receiver month
- “This Month” screen:
  - what I need to pay
  - who to pay and how
  - due date
  - upload proof + confirm sent
- “Incoming” screen (if receiver this month):
  - expected payers list
  - tick received per payer
  - create snooze/suppress exceptions (if enabled)
- Chat:
  - pool group chat
  - admin private chat
- Notifications center:
  - reminders
  - confirmations
  - disputes

### 7.3 Member Web (Optional)
Can be a lightweight web view mirroring mobile essentials.



## 8. Data Requirements (Entities and Relationships)

### 8.1 Minimum Entities
- Users (Account)
- Pools
- GuestMembers
- PoolMembers (join table)
- LuckyDrawEvents
- PayoutSchedule
- CyclePayoutConfig
- CyclePayoutReceivers
- MemberPaymentMethods
- CycleInstructionBatches
- PaymentInstructions
- PaymentEvidence
- PaymentConfirmations
- PaymentDisputes
- ReminderRules
- ReminderExceptions
- NotificationLog
- ChatThreads
- ChatMessages
- CyclePrivileges
- MemberReliability
- AuditLog

### 8.2 Key Relationship Rules
- PoolMembers links either User or GuestMember (mutually exclusive).
- PayoutSchedule is 1 row per (PoolId, CycleNo).
- CyclePayoutConfig overrides schedule for a cycle and can include N receivers.
- PaymentInstructions reference payer PoolMember and receiver PoolMember.
- Confirmations attach to PaymentInstructions.
- Evidence attaches to PaymentInstructions.
- ChatThreads can be pool-group or private user-to-user.
- CyclePrivileges assigns dynamic rights by cycle.



## 9. API Specification (Detailed Contract, v1)

### 9.1 API Design Conventions
- Base URL: `/api/v1`
- Auth: `Authorization: Bearer <JWT>`
- All responses use a standard envelope:
  - `statusCode`, `message`, `data`, `errors` (where applicable)
- OpenAPI (OAS) files must be generated for the full contract.  
OpenAPI defines a language-agnostic interface description for HTTP APIs. :contentReference[oaicite:11]{index=11}

### 9.2 Authentication
- `POST /auth/register`
- `POST /auth/login`
- `POST /auth/refresh` (recommended)
- `GET /users/me`
- `PATCH /users/me` (profile update; optional)

### 9.3 Pools
- `POST /pools` (Admin)
- `GET /pools` (Admin sees all; Member sees joined)
- `GET /pools/{poolId}`
- `PATCH /pools/{poolId}` (Admin; requires reason for critical fields)
- `POST /pools/{poolId}/activate` (Admin)
- `POST /pools/{poolId}/close` (Admin)

**Pool create payload (example)**
- name, currency, startMonth, cycleCount, fixedContributionAmount, dueDayOfMonth, reminderDay2, graceDays
- flags:
  - allowScheduleRegenerate
  - receiverCanSnooze
  - receiverCanSuppress
  - allowManualScheduleOverride
  - instructionStrategy (DiscreteMapping default)

### 9.4 Members
- `POST /pools/{poolId}/members/account` (Admin)
- `POST /pools/{poolId}/members/guest` (Admin)
- `GET /pools/{poolId}/members`
- `PATCH /pools/{poolId}/members/{poolMemberId}` (Admin)
- `POST /pools/{poolId}/members/{poolMemberId}/exit` (Admin)

### 9.5 Payment Methods (Receiver Details)
- `POST /pools/{poolId}/members/{poolMemberId}/payment-methods`
- `GET /pools/{poolId}/members/{poolMemberId}/payment-methods`
- `PATCH /payment-methods/{methodId}`
- `DELETE /payment-methods/{methodId}`

### 9.6 Lucky Draw and Schedule
- `POST /pools/{poolId}/schedule/lucky-draw` (Admin)
- `GET /pools/{poolId}/schedule`
- `POST /pools/{poolId}/schedule/lock` (Admin)
- `POST /pools/{poolId}/schedule/regenerate` (Admin; reason required)
- `PATCH /pools/{poolId}/schedule/{cycleNo}` (Admin; if manual override enabled)

### 9.7 Cycle Payout Config (Split)
- `POST /pools/{poolId}/cycles/{cycleNo}/payout` (Admin)
- `GET /pools/{poolId}/cycles/{cycleNo}/payout`
- `DELETE /pools/{poolId}/cycles/{cycleNo}/payout` (Admin; revert to default schedule receiver)

### 9.8 Instructions
- `POST /pools/{poolId}/cycles/{cycleNo}/instructions/preview` (Admin)
- `POST /pools/{poolId}/cycles/{cycleNo}/instructions/finalize` (Admin)
- `GET /pools/{poolId}/cycles/{cycleNo}/instructions` (Admin)
- `GET /members/me/instructions?poolId={poolId}&cycleNo={cycleNo}` (Member)
- `GET /members/me/incoming?poolId={poolId}&cycleNo={cycleNo}` (Receiver)

### 9.9 Confirmations, Evidence, Disputes
- `POST /payments/{instructionId}/payer-confirm` (Member; multipart for evidence)
- `POST /payments/{instructionId}/receiver-confirm` (ReceiverManager)
- `POST /payments/{instructionId}/admin-confirm` (Admin)
- `POST /payments/{instructionId}/dispute` (Member/Receiver/Admin)
- `POST /payments/{instructionId}/resolve` (Admin)
- `GET /payments/{instructionId}` (Authorized viewers only)
- `GET /payments/{instructionId}/evidence`
- `POST /payments/{instructionId}/evidence` (multipart)
- `DELETE /evidence/{evidenceId}` (Admin only; audited)

### 9.10 Notifications and Reminder Exceptions
- `GET /members/me/notifications`
- `POST /pools/{poolId}/cycles/{cycleNo}/reminders/run` (Admin)
- `POST /payments/{instructionId}/reminders/snooze` (ReceiverManager/Admin)
- `POST /payments/{instructionId}/reminders/suppress` (ReceiverManager/Admin)
- `GET /pools/{poolId}/cycles/{cycleNo}/reminders/log` (Admin)

### 9.11 Chat
- `POST /pools/{poolId}/chat/messages` (Members)
- `GET /pools/{poolId}/chat/messages`
- `POST /chat/private/{userId}/messages`
- `GET /chat/private/{userId}/messages`

### 9.12 Reliability
- `GET /pools/{poolId}/reliability` (Admin)
- `GET /pools/{poolId}/members/{poolMemberId}/reliability`
- `POST /pools/{poolId}/reliability/recalculate` (Admin)

### 9.13 Audit
- `GET /audit?poolId=&entityType=&dateFrom=&dateTo=` (Admin)



## 10. Real-Time Specification (SignalR)

SignalR enables server-side code to push content to clients instantly (real-time web functionality). :contentReference[oaicite:12]{index=12}

### 10.1 Hub
- Hub path: `/hubs/pool`
- Group naming: `Pool:{poolId}`

### 10.2 Events (Server → Client)
- `PoolJoined(poolId, userId)`
- `LuckyDrawStarted(poolId, byUserId, timestamp)`
- `LuckyDrawProgress(poolId, payload)` (optional animation steps)
- `LuckyDrawCompleted(poolId, schedule)`
- `InstructionsPreviewReady(poolId, cycleNo)`
- `InstructionsFinalized(poolId, cycleNo)`
- `PaymentStatusUpdated(poolId, cycleNo, instructionId, newStatus)`
- `ReceiverTickUpdated(poolId, cycleNo, receiverId, payerId, received)`
- `ChatMessageReceived(poolId, message)`
- `ReminderExceptionUpdated(poolId, cycleNo, instructionId)`
- `NotificationPushed(userId, notification)`

### 10.3 Group Join Rules
- Only authenticated account users can connect.
- Users can join only pools they belong to (enforce on server).
- Admin can join any pool.



## 11. Non-Functional Requirements (NFR)

### 11.1 Security (Mandatory)
- NFR-SEC-1: All endpoints require auth except register/login.
- NFR-SEC-2: Role-based authorization enforced (Admin vs Member vs ReceiverManager).
- NFR-SEC-3: File uploads validated (size limits, MIME type allowlist, malware scanning optional).
- NFR-SEC-4: Sensitive values (payment identifiers, tokens) protected at rest where feasible.
- NFR-SEC-5: Audit logs must be tamper-resistant at application level.
- NFR-SEC-6: Follow OWASP ASVS as the baseline for secure requirements and verification. :contentReference[oaicite:13]{index=13}

### 11.2 Reliability and Availability
- NFR-REL-1: System should recover gracefully from server restarts; no data loss.
- NFR-REL-2: Background jobs (reminders, reliability calc) must be retry-safe and idempotent.
- NFR-REL-3: Evidence file storage must support backup/retention policy.

### 11.3 Performance
- NFR-PERF-1: Core APIs should respond within acceptable time under typical load (define SLA in engineering).
- NFR-PERF-2: SignalR events should be delivered promptly for connected clients.
- NFR-PERF-3: Instruction generation should complete within seconds for typical pool sizes (e.g., 10–200 members).

### 11.4 Usability
- NFR-UX-1: Member “This Month” flow must be minimal steps.
- NFR-UX-2: Receiver ticking must be bulk-friendly (quick tick UI).
- NFR-UX-3: Admin dashboards must show progress at a glance.

### 11.5 Observability
- NFR-OBS-1: Structured logging with correlation IDs across requests.
- NFR-OBS-2: Metrics: number of reminders sent, confirmation latency, disputes count.
- NFR-OBS-3: Error reporting and alerting for job failures.

### 11.6 Compatibility
- NFR-COMP-1: Flutter app supports modern Android/iOS versions (define minimum versions during build).
- NFR-COMP-2: Web portal supports modern browsers (Chrome/Edge/Firefox).



## 12. Background Jobs and Scheduling

### 12.1 Reminder Job
- Runs daily (or on configured schedule).
- For each active pool:
  - Identify current cycle.
  - Identify unpaid instructions whose reminder date is today.
  - Apply reminder exceptions (snooze/suppress).
  - Send notifications and log results.

### 12.2 Reliability Recalculation Job
- Runs nightly and/or after confirmation events.
- Recomputes reliability badges per member.

### 12.3 Privilege Rotation Job
- At cycle transition:
  - revoke ReceiverManager privileges from previous cycle receiver(s)
  - grant to new cycle receiver(s)



## 13. Edge Cases and Exception Handling (Must Be Implemented)

### 13.1 Member Count Changes After Draw
- If member exits before pool starts:
  - admin can regenerate schedule with reason.
- If member exits mid-pool:
  - policy needed:
    - Option A: exclude from future instructions
    - Option B: admin manual settlement
  - system must preserve history.

### 13.2 Multiple Receivers and Evidence Privacy
- Evidence attached to instruction is visible only to:
  - payer
  - receiver
  - admin
- Not visible to unrelated members in group chat unless explicitly shared.

### 13.3 Conflicting Confirmations
- If payer marks sent but receiver marks not received:
  - system enters Disputed
  - admin must resolve

### 13.4 Guest “On Behalf” Abuse Prevention
- Every admin action on behalf requires:
  - reason/note
  - audit log entry

### 13.5 Rounding Disputes
- Rounding rule must be transparent in admin “instructions preview”.
- Admin must be able to review totals:
  - per receiver target vs computed
  - per payer totals



## 14. Acceptance Criteria (Build-Complete Definition)

### 14.1 Pool Lifecycle
- Admin can create, activate, and close pools.
- Admin can add account + guest members and see combined list.

### 14.2 Lucky Draw
- Admin runs draw; connected members see draw completion live.
- Schedule persists; regeneration requires reason and audit.

### 14.3 Split Payout and Instructions
- Admin configures split payout (e.g., 70/30).
- Instructions generated correctly:
  - totals match percentages (within rounding policy)
  - each payer sees exactly what to pay and to whom

### 14.4 Confirmations and Evidence
- Payer can confirm and upload proof.
- Receiver can tick received per payer.
- Admin can confirm on behalf of guests.
- Dispute workflow works end-to-end with resolution.

### 14.5 Notifications
- Unpaid members get reminders on configured days.
- Snooze/suppress prevents reminders as configured.
- Receiver privileges rotate automatically month to month.

### 14.6 Reliability Scoring
- Member badges compute according to thresholds.
- Admin can filter members by badge color.

### 14.7 Auditability
- All critical actions appear in audit logs with:
  - actor
  - timestamp
  - before/after (where relevant)
  - reason



## 15. Implementation Guidance (Stack-Aligned)

### 15.1 ASP.NET Core
ASP.NET Core is designed for high performance and enterprise workloads. :contentReference[oaicite:14]{index=14}

### 15.2 SignalR
SignalR simplifies adding real-time web functionality where server pushes updates to clients instantly. :contentReference[oaicite:15]{index=15}

### 15.3 Flutter
Flutter provides multiplatform app development from a single codebase. :contentReference[oaicite:16]{index=16}

### 15.4 Dapper
Dapper enhances ADO.NET connections with a simple, efficient API for executing SQL and mapping results. :contentReference[oaicite:17]{index=17}

### 15.5 OpenAPI
OpenAPI defines a standard interface description for HTTP APIs enabling documentation, client generation, and testing. :contentReference[oaicite:18]{index=18}



## 16. References (Links)
- ISO/IEC/IEEE 29148 (ISO listing): https://www.iso.org/standard/72089.html :contentReference[oaicite:19]{index=19}  
- IEEE 29148 standard page: https://standards.ieee.org/standard/29148-2018.html :contentReference[oaicite:20]{index=20}  
- ASP.NET Core SignalR Introduction: https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction :contentReference[oaicite:21]{index=21}  
- SignalR Tutorial (chat example): https://learn.microsoft.com/en-us/aspnet/core/tutorials/signalr :contentReference[oaicite:22]{index=22}  
- Flutter docs: https://docs.flutter.dev/ :contentReference[oaicite:23]{index=23}  
- Flutter architecture overview: https://docs.flutter.dev/resources/architectural-overview :contentReference[oaicite:24]{index=24}  
- Dapper repository: https://github.com/DapperLib/Dapper :contentReference[oaicite:25]{index=25}  
- OWASP ASVS (official): https://owasp.org/www-project-application-security-verification-standard/ :contentReference[oaicite:26]{index=26}  
- OpenAPI Spec (official): https://spec.openapis.org/ :contentReference[oaicite:27]{index=27}  
- OpenAPI 3.1 schema page: https://swagger.io/specification/ :contentReference[oaicite:28]{index=28}  



## 17. Appendix A — Concrete Examples

### A.1 Split Payout Example (70/30)
**Pool:** 10 members, each contributes 10,000 PKR  
**Total pot:** 100,000 PKR  
**Receivers:**  
- Receiver A: 70% → 70,000  
- Receiver B: 30% → 30,000  

**Discrete mapping output:**  
- 7 payers assigned to Receiver A (7 × 10,000 = 70,000)  
- 3 payers assigned to Receiver B (3 × 10,000 = 30,000)  

Each payer sees a single instruction:
- “Pay 10,000 PKR to Receiver A/B by 5th.”

### A.2 Reminder Exception Example
Member messages receiver privately:
- “I will pay on 12th.”

Receiver (this month) sets:
- Snooze until 12th
- Reason: “Member informed late payment”
System does not remind on 10th; will remind after 12th if still unpaid.

### A.3 Guest Member Confirmation Example
Guest payer “Uncle Ahmed” pays in cash and cannot upload proof.
Admin records:
- PayerConfirmed (on behalf)
- Note: “Paid cash to receiver; confirmed verbally”
System displays “Confirmed by Admin on behalf of Guest” and audits the action.



**End of Document**
