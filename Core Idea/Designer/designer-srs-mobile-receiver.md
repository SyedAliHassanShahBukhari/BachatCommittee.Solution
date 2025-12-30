# Designer SRS – Mobile App (Receiver of the Month)
## Bachat Committee Management System

Document ID: BCS-DES-MOB-RECEIVER-002  
Version: 2.1 (Skeleton + Dialogs Included)  
Audience: Mobile UI/UX Designers  
Platform: Flutter (Android & iOS)

Primary Design Objective:
Enable the receiver to track, confirm, and manage incoming payments with minimal effort and maximum clarity.

---

## 1. Role Definition & Scope

### Receiver-of-the-Month
A temporary, system-assigned role active only for the current cycle.

**Receiver CAN**
- View expected incoming payments
- Mark payments as received
- Snooze or suppress reminders (if allowed)

**Receiver CANNOT**
- Edit pool configuration
- Modify schedules
- Resolve disputes (unless also admin)

---

## 2. Navigation Behavior (Receiver-Specific)

### Bottom Navigation Tabs
1. Home  
2. This Month  
3. Incoming *(visible only during active receiver cycle)*  
4. Chat  
5. Profile  

**Visibility Rules**
- “Incoming” tab appears only when the user is one of the receiver(s) for the current cycle
- Tab is removed automatically after cycle ends or when receiver privileges rotate

---

## 3. Incoming Screen (Core Receiver Flow)

### Purpose
Answer immediately:
- Who needs to pay me?
- Who has already paid?
- Who is late?

---

## 3.1 Page Skeleton (Incoming Screen)

### Top App Bar 
- Screen Title: Incoming Payments
- Pool Selector (if multiple pools)
- Optional: Info icon (explains receiver role & expiry)

###  Receiver Summary Card  <-- PRIMARY FOCUS (Above the fold)
- Expected Total Amount (PKR)
- Received Total Amount (PKR)
- Progress Bar (Received / Expected)
- Cycle Info (Cycle X – Month Year)
- Optional: “Unpaid: N” indicator

###  Utilities Row 
- Search input: "Search payer..."
- Filter chips:
  - All
  - Unpaid
  - Late
  - Disputed

###  Incoming Payments List 
- Scrollable payer rows
- Each row includes:
  - Payer Name
  - Member Type Badge (Account / Guest)
  - Amount
  - Status Badge (Pending / Received / Late / Disputed)
  - Primary action: Tick Received (enabled only if Pending/Late)
  - Secondary action (if enabled): Snooze / Suppress

###  Sticky Footer / Info Banner 
- “Receiver privileges end after this cycle.”
- Optional link: “Learn more”

---

## 3.2 Receiver Summary Card

**Visual Priority:** Highest  
**Position:** Above the fold

**Contents**
- Expected Total (PKR)
- Received Total (PKR)
- Animated progress bar
- Cycle label

**Micro-interactions**
- Progress bar animates on updates
- Tooltip explaining totals calculation

---

## 3.3 Incoming Payments List

### Payer Row Structure

**Left**
- Payer Name (bold)
- Member Type Icon:
  - Account
  - Guest

**Center**
- Amount
- Status Badge:
  - Pending
  - Received
  - Late
  - Disputed

**Right**
- Tick Received button (enabled only if Pending)

---

## 3.4 Tick Received Interaction

### Confirmation Dialog

[ Dialog Header ]
Title: Confirm Payment Received

[ Dialog Body ]
Message:
“I confirm I received PKR 10,000 from Ahmed.”

Optional Details (small text):
Cycle: 3 (March 2026)
Pool: Office Committee 2026

[ Required Checkbox ]
□ I confirm receipt

[ Buttons ]
Primary: Confirm
Secondary: Cancel

[ Loading State ]
- Confirm button shows spinner
- Buttons disabled while saving

[ Success Feedback ]
- Toast: “Payment marked as received”
- Row status changes to Received
- Progress bar updates

[ Failure Feedback ]
- Inline error message in dialog OR ErrorBanner on page:
  “Unable to confirm right now. Please try again.”
- Confirm button becomes active again

**After Confirm**
- Status updates to Received
- Progress bar updates
- Success toast: “Payment marked as received”

**Failure**
- Inline Error Banner shown

---

## 4. Reminder Exceptions (If Enabled)

### Permission Rule
Visible only if pool configuration allows receiver control.

---

## 4.1 Snooze Reminder Dialog

[ Dialog Header ]
Title: Snooze Reminder

[ Body ]
Help text:
“Snooze reminders for this payer until a specific date.”

[ Required Field 1 ]
Reason (multiline text input)
Placeholder: “Member informed they will pay late…”

[ Required Field 2 ]
Snooze Until (date picker)

[ Buttons ]
Primary: Confirm Snooze
Secondary: Cancel

[ Loading State ]
- Primary shows spinner
- Inputs disabled while saving

[ Success Feedback ]
- Toast: “Reminder snoozed until 12 Mar 2026”
- Row shows Snoozed indicator badge:
  “Snoozed until 12 Mar”

[ Failure Feedback ]
- Inline error text:
  “Could not save snooze. Try again.”

**Behavior**
- No reminders until selected date
- Snooze indicator shown on payer row

---

## 4.2 Suppress Reminder Dialog

[ Dialog Header ]
Title: Suppress Reminder

[ Body ]
Warning text (prominent):
“No reminders will be sent to this payer for the current cycle.”

[ Required Field ]
Reason (multiline text input)
Placeholder: “Already agreed payment schedule…”

[ Buttons ]
Primary: Suppress
Secondary: Cancel

[ Loading State ]
- Primary shows spinner
- Inputs disabled while saving

[ Success Feedback ]
- Toast: “Reminder suppressed for this cycle”
- Row shows Suppressed badge:
  “Suppressed”

[ Failure Feedback ]
- Inline error text:
  “Could not suppress reminder. Try again.”

---

## 5. Guest Member Handling

**Visual Rules**
- Guest badge shown
- No chat icon
- No evidence upload

**Confirmation Rules**
- Receiver may mark received
- Admin may confirm on behalf
- UI label: “Confirmed by Admin on behalf of Guest”

---

## 6. Status & Visual Language

### Payment Status Colors
- Pending → Gray
- Received → Green
- Late → Orange
- Disputed → Red

### Disabled States
- Tick Received disabled after confirmation
- Snooze/Suppress disabled after payment received

---

## 7. Edge Cases (Must Be Designed)

- Admin confirms before receiver
- Conflicting confirmations → Disputed
- Receiver role removed mid-session
- Offline confirmation → Syncing indicator

---

## 8. Sample Data (For Mockups)

- Expected Total: PKR 100,000
- Received: PKR 60,000
- Payer: Ahmed (Account)
- Amount: PKR 10,000
- Status: Pending

---

## 9. Designer Deliverables

- Incoming screen (all states)
- Confirmation dialogs
- Snooze/Suppress dialogs
- Empty state
- Error states
- Component variants

---

END OF MOBILE RECEIVER DESIGNER SRS (v2)
