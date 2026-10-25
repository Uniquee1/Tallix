# Tallix Requirements

## 1. Product Overview

Tallix is a personal lending and borrowing ledger.

Users can record financial transactions involving other people and determine how much they currently owe or are owed.

The application will be available as:

* Web application
* Mobile application

Both clients consume the same backend API.

The mobile application will use an **offline-first architecture**, allowing users to view and create records without an internet connection. Changes will automatically synchronize with the cloud when connectivity is restored.

---

# 2. User Types

## 2.1 Registered User

A registered user can:

* Manage their profile
* Create contacts
* Record ledger transactions
* View balances
* View transaction history
* Edit permitted records
* Reverse transactions
* Manage attachments
* Configure reminders

---

# 3. Functional Requirements

## FR-001 Authentication

The system shall allow users to authenticate securely.

The system shall:

* Authenticate users through an external identity provider
* Maintain authenticated sessions/tokens
* Associate application data with the authenticated user
* Reject unauthorized API requests

The mobile application should retain the minimum authentication information required to support offline access securely.

---

# FR-002 User Profile

Users shall be able to view and update:

* Display name
* Email
* Preferred currency
* Time zone
* Notification preferences

---

# FR-003 Contacts

Users shall be able to create contacts.

A contact may contain:

* Name
* Phone number
* Email address
* Notes
* Optional profile image

A contact belongs to exactly one application user.

Users shall only be able to access their own contacts.

The mobile application shall allow contacts to be viewed and created while offline.

---

# FR-004 Ledger Entries

Users shall be able to create ledger entries.

Each entry shall contain:

* Contact
* Transaction type
* Amount
* Currency
* Date/time
* Optional due date
* Description
* Created timestamp

Supported transaction types:

```text
LENT
BORROWED
PAYMENT_RECEIVED
PAYMENT_MADE
ADJUSTMENT
```

Ledger entries are immutable once posted. There is no edit operation. Mistakes are corrected by reversing the entry and recording a new one (see FR-009).

The mobile application shall allow ledger entries to be created while offline.

---

# FR-005 Amount Validation

The system shall:

* Reject zero-value transactions
* Reject negative amounts where not explicitly supported
* Validate decimal precision
* Validate currency
* Prevent invalid transaction states

Financial calculations shall use decimal types rather than floating-point types.

---

# FR-006 Balance Calculation

The system shall calculate a contact's current balance from ledger entries.

Example:

```text
LENT               +5000
PAYMENT_RECEIVED   -1000
LENT               +2000
-----------------------
Balance             6000
```

The application should avoid using a manually maintained balance as the authoritative financial record.

The mobile application shall calculate and display balances from its local data while offline.

---

# FR-007 Partial Payments

Users shall be able to record partial payments.

Example:

```text
Original amount: ₱5,000
Payment:         ₱2,000
Remaining:       ₱3,000
```

Multiple payments shall be supported.

Partial payments shall work offline on mobile.

---

# FR-008 Transaction History

Users shall be able to view chronological transaction history.

The history shall display:

* Date
* Type
* Amount
* Description
* Contact
* Resulting balance where applicable

Previously synchronized records shall remain available on mobile when offline.

---

# FR-009 Transaction Correction

Financial records should not normally be physically deleted.

Instead, the system should support:

* Reversal
* Correction
* Audit history

A reversed transaction must remain traceable.

Offline-created corrections shall be synchronized when connectivity is restored.

A reversal is recorded as a new ADJUSTMENT entry whose amount is the exact opposite of the original entry's effect on the balance, and which links back to the original. An entry can be reversed at most once, and a reversal cannot itself be reversed.
---

# FR-010 Due Dates

Users may optionally specify a due date.

The application shall be able to identify:

* Upcoming due dates
* Due today
* Overdue entries

Due dates must continue to be available while the mobile application is offline.

---

# FR-011 Dashboard

The dashboard shall display:

* Total owed to the user
* Total the user owes
* Net position
* Number of active contacts
* Upcoming due dates
* Overdue entries
* Recent transactions

The mobile dashboard shall work offline using locally stored data.

The UI should indicate when displayed information has not yet synchronized with the server.

---

# FR-012 Search

Users shall be able to search contacts by:

* Name
* Email
* Phone number

Users shall be able to search/filter ledger entries by:

* Contact
* Transaction type
* Date range
* Status

Mobile search shall work against locally stored records while offline.

---

# FR-013 Attachments

Users may attach documents/images to ledger records.

Examples:

* Receipt
* Screenshot
* Written agreement
* Payment proof

Attachments shall be stored in Azure Blob Storage.

Attachment uploads may be queued while the mobile application is offline and uploaded when connectivity is restored.

---

# FR-014 Notifications

The future notification system shall support:

* Upcoming due-date reminders
* Overdue reminders
* Payment confirmations

Notifications shall be configurable by the user.

Local notifications may be generated by the mobile application even when offline.

---

# FR-015 Web Application

The web application shall provide:

* Authentication
* Dashboard
* Contact management
* Ledger management
* Transaction history
* Search/filtering
* Responsive UI

The web application may initially require an internet connection.

Offline-first behavior is a core requirement for the mobile application.

---

# FR-016 Mobile Application

The mobile application shall provide:

* Authentication
* Dashboard
* Contact management
* Ledger entries
* Payment recording
* Balance display
* Transaction history
* Offline access
* Background synchronization where supported
* Synchronization status
* Retry handling

---

# FR-017 Offline Mode

The mobile application shall use an offline-first architecture.

When the device has no internet connection, the user shall still be able to:

* View previously synchronized contacts
* View previously synchronized ledger entries
* View balances
* Search local records
* Create contacts
* Create ledger entries
* Record payments
* Record supported corrections
* View due dates

The user shall not be required to wait for the API before completing an offline-supported action.

---

# FR-018 Local Data Storage

The mobile application shall maintain a local database/cache containing the records required for offline operation.

The local database shall contain, at minimum:

```text
contacts
ledger_entries
sync_queue
sync_metadata
```

Sensitive authentication information shall not be stored as ordinary application data.

---

# FR-019 Synchronization Queue

Offline mutations shall be placed into a local synchronization queue.

Example:

```text
User creates transaction
        ↓
Save locally
        ↓
Update local balance
        ↓
Add operation to sync queue
        ↓
Internet unavailable
        ↓
Wait
        ↓
Internet restored
        ↓
Synchronize with API
```

Each queued operation should contain enough information to retry the operation safely.

---

# FR-020 Synchronization

When connectivity is restored, the mobile application shall attempt to synchronize pending changes.

Synchronization shall:

* Detect pending local operations
* Send them to the API
* Mark successful operations as synchronized
* Retry temporary failures
* Preserve failed operations for later retry
* Update local records using server responses
* Avoid creating duplicate records

---

# FR-021 Idempotency

Offline synchronization shall prevent duplicate server-side records.

Client-created operations should have a unique client-generated identifier.

Example:

```text
clientOperationId:
"7cbd8b5e-..."
```

The API should use this identifier to recognize previously processed operations.

If a synchronization request is retried, the API should not create a second transaction.

---

# FR-022 Sync Status

The mobile application shall communicate synchronization state to the user.

Possible states:

```text
Synced
Offline
Syncing
Pending
Sync error
```

Example:

```text
✓ Synced
```

```text
☁ 3 changes waiting to sync
```

```text
⚠ Sync failed — will retry
```

The UI should avoid making the user think a locally created transaction has already reached the server when it has not.

---

# FR-023 Connectivity Detection

The mobile application shall detect network connectivity changes.

When connectivity is lost:

```text
Online
  ↓
Offline
  ↓
Use local database
```

When connectivity returns:

```text
Offline
  ↓
Online
  ↓
Start synchronization
```

Connectivity detection must not be treated as proof that the API is reachable.

The application should still handle API connection failures gracefully.

---

# FR-024 Sync Conflicts

The system shall define a conflict strategy.

The initial strategy should favor the ledger's append-oriented design.

For new transactions:

```text
Client creates transaction
        ↓
Unique clientOperationId
        ↓
Server accepts once
```

For records that can be edited, the system should use:

* Server timestamps
* Updated timestamps
* Record version information where appropriate

Conflict behavior must be deterministic.

The application should never silently overwrite a user's data without a defined rule.

---

# FR-025 Offline Indicators

The mobile application shall show when it is operating offline.

Example:

```text
Offline
Changes will sync when you're back online.
```

The indicator should not prevent the user from using offline-supported features.

---

# FR-026 Sync Failure Handling

Temporary synchronization failures shall not cause local records to disappear.

If synchronization fails:

```text
Local record
     ↓
Remains available
     ↓
Marked pending/error
     ↓
Retry later
```

The user should be able to retry synchronization manually.

---

# 4. Non-Functional Requirements

## NFR-001 Security

The system shall:

* Use HTTPS
* Authenticate API requests
* Authorize resource access
* Isolate users' data
* Validate input
* Avoid storing secrets in source control
* Store cloud secrets in Azure Key Vault

Mobile local data should be protected using platform-appropriate secure storage/database capabilities.

---

# NFR-002 Availability

The production environment should be designed for reasonable availability using managed Azure services.

The mobile application must remain useful when the backend is temporarily unavailable.

---

# NFR-003 Performance

Typical API requests should target:

```text
p95 < 500ms
```

under normal application load.

Local mobile reads should not depend on network latency.

---

# NFR-004 Scalability

The architecture should allow horizontal scaling of the API without changing the core application design.

---

# NFR-005 Maintainability

The backend shall follow clear separation of:

* API
* Application
* Domain
* Infrastructure

The frontend should maintain reusable components and services.

The mobile synchronization system should be isolated from UI components as much as practical.

---

# NFR-006 Observability

Production shall provide:

* Structured logging
* Error tracking
* Metrics
* Health checks
* Application performance monitoring

Synchronization failures should produce useful diagnostic information without exposing sensitive user data.

---

# NFR-007 Disaster Recovery

Database backups shall be enabled through Azure-managed PostgreSQL capabilities.

Infrastructure shall be reproducible using Terraform.

Local mobile data shall not be considered the permanent source of truth.

---

# NFR-008 Portability

The backend shall be containerized using Docker.

The application should be capable of running locally without depending exclusively on Azure.

---

# 5. Business Rules

## BR-001

Every ledger entry belongs to exactly one application user.

## BR-002

Users cannot access another user's contacts or ledger entries.

## BR-003

An entry must belong to a contact owned by the same user.

## BR-004

Amounts use decimal precision appropriate for monetary values.

## BR-005

Financial records should be immutable after posting where practical.

Corrections should be represented through reversal/correction records.

## BR-006

A contact may have both positive and negative transactions over time.

Example:

```text
They owe me ₱5,000
I borrow ₱1,000 from them

Net balance = ₱4,000 owed to me
```

## BR-007

A zero balance means there is no outstanding amount between the user and contact.

## BR-008

An offline-created transaction is considered locally recorded but not cloud-synchronized until the API confirms it.

---

# 6. API Requirements

The API should expose RESTful endpoints.

Example:

```text
GET    /api/contacts
POST   /api/contacts
GET    /api/contacts/{id}
PUT    /api/contacts/{id}
DELETE /api/contacts/{id}

GET    /api/contacts/{id}/ledger
POST   /api/contacts/{id}/ledger

GET    /api/ledger/{id}
POST   /api/ledger/{id}/reverse

GET    /api/dashboard

POST   /api/sync
```

The synchronization endpoint may evolve into more specialized endpoints as implementation details become clearer.

The API shall expose an OpenAPI specification.

---

# 7. Testing Requirements

The system shall contain:

### Unit Tests

For:

* Balance calculations
* Transaction validation
* Business rules
* Domain logic
* Sync queue behavior
* Conflict handling

### Integration Tests

For:

* API
* Database
* Authentication/authorization boundaries
* Idempotent synchronization

### End-to-End Tests

For important user journeys:

```text
Login
→ Create contact
→ Record lending
→ Record payment
→ Verify balance
```

Offline workflow:

```text
Load data
→ Disable network
→ Create transaction
→ Verify local balance
→ Restore network
→ Synchronize
→ Verify server record
```

---

# 8. DevOps Requirements

The project shall use:

* Git
* GitHub
* Pull requests
* GitHub Actions
* Docker
* Terraform
* Automated tests
* Automated builds
* Automated deployments

CI shall execute automatically for pull requests.

Production deployment shall occur only through an approved deployment workflow.

---

# 9. Cloud Requirements

Production infrastructure shall use Azure.

Target services:

* Azure Container Apps
* Azure Database for PostgreSQL
* Azure Container Registry
* Azure Blob Storage
* Azure Key Vault
* Azure Application Insights
* Azure Monitor / Log Analytics

---

# 10. Acceptance Criteria

The MVP is considered complete when a user can:

1. Register/login.
2. Create a contact.
3. Record money they lent.
4. Record money they borrowed.
5. Record partial payments.
6. See the resulting balance.
7. View transaction history.
8. See dashboard totals.
9. Access the same information from web and mobile.
10. Use the mobile application with no internet connection.
11. Create supported records while offline.
12. See locally calculated balances while offline.
13. Restore internet connectivity.
14. Automatically synchronize pending records.
15. Retry failed synchronization.
16. Avoid duplicate server-side records.
17. See synchronization status.
18. Run automated tests.
19. Deploy the system to Azure using the documented CI/CD process.
20. Provision the infrastructure using Terraform.
21. Monitor the deployed application.
