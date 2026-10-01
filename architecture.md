# Tallix Architecture

## 1. Architectural Goals

The architecture is designed around five goals:

1. Keep the application simple enough to maintain.
2. Provide strong separation of concerns.
3. Support both web and mobile clients.
4. Demonstrate modern cloud and DevOps practices.
5. Allow the system to evolve without premature complexity.

The primary architectural style is a **modular monolith**.

---

# 2. High-Level Architecture

```text
                         INTERNET
                             │
             ┌───────────────┴────────────────┐
             │                                │
             ▼                                ▼
     ┌────────────────┐              ┌────────────────┐
     │   Next.js Web  │              │ React Native   │
     │     Client     │              │ Mobile Client  │
     └───────┬────────┘              └───────┬────────┘
             │                               │
             └───────────────┬───────────────┘
                             │
                           HTTPS
                             │
                             ▼
                  ┌─────────────────────┐
                  │   Azure Container   │
                  │       Apps          │
                  │                     │
                  │  ASP.NET Core API   │
                  └──────────┬──────────┘
                             │
             ┌───────────────┼────────────────┐
             │               │                │
             ▼               ▼                ▼
       PostgreSQL       Blob Storage       Key Vault
             │
             │
             ▼
      Application Insights
      Azure Monitor
```

---

# 3. Architectural Style

## Modular Monolith

The backend is one deployable application but internally separated into modules.

```text
Tallix.Api
Tallix.Application
Tallix.Domain
Tallix.Infrastructure
```

Potential domain modules:

```text
Identity
Users
Contacts
Ledger
Attachments
Notifications
```

The initial application will not use microservices.

---

# 4. Backend Layers

## API Layer

Responsibilities:

* HTTP endpoints
* Authentication
* Authorization
* Request validation
* Response mapping
* OpenAPI

Example:

```text
POST /api/contacts/{id}/ledger
```

The API layer should not contain complex business logic.

---

## Application Layer

Responsibilities:

* Use cases
* Application orchestration
* Commands
* Queries
* DTOs
* Validation
* Transaction boundaries

Example use cases:

```text
CreateContact
RecordLedgerEntry
RecordPayment
GetContactBalance
GetDashboard
ReverseLedgerEntry
```

---

## Domain Layer

Contains business rules and core concepts.

Example:

```text
Contact
LedgerEntry
Money
TransactionType
Balance
```

Business calculations should live here where practical.

---

## Infrastructure Layer

Responsibilities:

* Entity Framework Core
* PostgreSQL
* Blob Storage
* Identity provider integration
* Azure services
* Logging
* External services

---

# 5. Domain Model

The central concept is the ledger entry.

```text
User
 │
 ├── Contact
 │      │
 │      └── LedgerEntry
 │
 └── Preferences
```

A simplified model:

```text
User
 ├── Id
 ├── Email
 └── DisplayName

Contact
 ├── Id
 ├── UserId
 ├── Name
 ├── Phone
 └── Email

LedgerEntry
 ├── Id
 ├── UserId
 ├── ContactId
 ├── Type
 ├── Amount
 ├── Currency
 ├── Description
 ├── OccurredAt
 ├── DueDate
 └── CreatedAt
```

---

# 6. Ledger Model

The application treats transactions as the source of truth.

For example:

```text
Contact: Juan

LENT               5000
PAYMENT_RECEIVED   1000
LENT               2000
-----------------------
Balance             6000
```

The balance is calculated rather than treated as the authoritative database field.

---

# 7. Money Representation

Money should not use floating-point arithmetic.

The backend should use:

```text
decimal
```

and the database should use an appropriate numeric/decimal type.

Example:

```text
NUMERIC(19,4)
```

The exact precision may be adjusted based on supported currencies.

---

# 8. Database Architecture

PostgreSQL is the primary database.

Initial tables:

```text
users
contacts
ledger_entries
attachments
audit_entries
```

The application should use foreign keys and database constraints to enforce integrity.

Indexes should initially target:

```text
contacts.user_id
ledger_entries.user_id
ledger_entries.contact_id
ledger_entries.occurred_at
ledger_entries.due_date
```

---

# 9. Data Isolation

Every user-owned resource contains a user ownership relationship.

Example:

```text
LedgerEntry.UserId
Contact.UserId
```

When accessing a resource, the API must verify ownership.

A user must never be able to access another user's data simply by changing an ID in an HTTP request.

---

# 10. Authentication

Authentication is handled through an external identity provider.

The application receives an authenticated identity/token.

The API maps that identity to an application user.

Conceptually:

```text
Identity Provider
       │
       ▼
Access Token
       │
       ▼
ASP.NET Core
       │
       ▼
Application User
```

The API is responsible for authorization.

---

# 11. API Architecture

The API follows REST principles.

Example:

```text
GET /api/contacts
GET /api/contacts/{id}

POST /api/contacts

GET /api/contacts/{id}/ledger
POST /api/contacts/{id}/ledger

GET /api/ledger/{id}
POST /api/ledger/{id}/reverse

GET /api/dashboard
```

OpenAPI is generated from the API.

---

# 12. Client Architecture

## Web

The web application uses:

```text
Next.js
React
TypeScript
Tailwind CSS
TanStack Query
```

Suggested structure:

```text
apps/web/

app/
components/
features/
lib/
hooks/
services/
types/
```

Feature-oriented organization is preferred over a single global components directory.

---

# 13. Mobile

The mobile application uses:

```text
React Native
Expo
TypeScript
```

Suggested structure:

```text
apps/mobile/

app/
components/
features/
services/
hooks/
types/
```

The mobile client communicates exclusively through the API.

---

# 14. API Client

Web and mobile should use generated or centrally defined API contracts where practical.

The API contract should be represented through OpenAPI.

This reduces differences between:

```text
Backend
Web client
Mobile client
```

---

# 15. File Storage

Files are stored in Azure Blob Storage.

The database stores metadata:

```text
Attachment
 ├── Id
 ├── LedgerEntryId
 ├── BlobPath
 ├── FileName
 ├── ContentType
 └── CreatedAt
```

The application should not store large binary files directly in PostgreSQL.

---

# 16. Azure Architecture

Production:

```text
Azure Resource Group
│
├── Container Apps Environment
│   ├── API
│   └── Web
│
├── Azure Container Registry
│
├── Azure Database for PostgreSQL
│
├── Storage Account
│   └── Blob Container
│
├── Key Vault
│
├── Application Insights
│
└── Log Analytics
```

---

# 17. Containerization

The API and web application are containerized.

```text
Docker
 ├── API image
 └── Web image
```

Images are pushed to Azure Container Registry.

Azure Container Apps runs the images.

---

# 18. Infrastructure as Code

Terraform manages Azure infrastructure.

```text
infra/terraform/

main.tf
variables.tf
outputs.tf

modules/
├── container-app/
├── database/
├── storage/
├── keyvault/
└── monitoring/
```

Infrastructure should be reproducible.

---

# 19. CI/CD

GitHub Actions provides automation.

## Pull Request

```text
Pull Request
     │
     ├── Frontend lint
     ├── Backend build
     ├── Unit tests
     ├── Integration tests
     ├── E2E tests
     ├── Dependency scan
     └── Docker build
```

## Main

```text
main
 │
 ▼
Build
 │
 ▼
Container Registry
 │
 ▼
Staging
 │
 ▼
Approval
 │
 ▼
Production
```

---

# 20. Observability

Application Insights collects:

* Requests
* Exceptions
* Dependencies
* Performance information

Application logs should use structured logging.

Example:

```json
{
  "event": "LedgerEntryCreated",
  "ledgerEntryId": "...",
  "contactId": "...",
  "amount": 5000
}
```

Sensitive information should not be logged.

---

# 21. Health Checks

The API should expose health endpoints.

```text
/health
/health/ready
```

Readiness should verify required dependencies where appropriate.

---

# 22. Security Architecture

Security controls include:

```text
HTTPS
  ↓
Authentication
  ↓
Authorization
  ↓
Input Validation
  ↓
Application Logic
  ↓
Database
```

Additional controls:

* Key Vault
* Managed identities
* Private storage
* Database encryption
* Dependency scanning
* Rate limiting
* Security headers
* Audit logging

---

# 23. Error Handling

The API should return consistent error responses.

A standard problem-details format should be preferred.

Example:

```json
{
  "type": "https://example.com/errors/validation",
  "title": "Validation failed",
  "status": 400,
  "detail": "The amount must be greater than zero."
}
```

---

# 24. Deployment Environments

Three logical environments:

```text
Development
Staging
Production
```

Development may run locally.

Staging and production are hosted in Azure.

Configuration and secrets are environment-specific.

---

# 25. Architectural Principles

Tallix follows these principles:

1. Keep the domain model simple.
2. Keep financial records auditable.
3. Prefer immutable financial transactions.
4. Avoid premature microservices.
5. Automate infrastructure.
6. Automate testing.
7. Automate deployment.
8. Keep secrets out of source control.
9. Make observability a first-class concern.
10. Prefer managed cloud services where they reduce operational overhead.
