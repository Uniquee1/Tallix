# Tallix Development Plan

## 1. Project Objective

Build and deploy Tallix as a production-style portfolio project demonstrating:

* Full-stack development
* Mobile development
* API design
* Relational database design
* Cloud architecture
* Azure
* Docker
* Terraform
* CI/CD
* Automated testing
* Security
* Observability
* Documentation

The project should remain small enough to finish while providing enough engineering depth to demonstrate professional development practices.

---

# 2. Development Strategy

The project will be developed incrementally.

```text
Foundation
   ↓
Domain
   ↓
Backend
   ↓
Web
   ↓
Mobile
   ↓
Testing
   ↓
Cloud
   ↓
CI/CD
   ↓
Observability
   ↓
Hardening
```

The application should reach a usable state early rather than attempting to build every feature before deployment.

---

# 3. Phase 0 — Project Definition

## Goals

Establish the repository and development conventions.

### Tasks

* Create GitHub repository
* Create README
* Create requirements document
* Create architecture document
* Create development plan
* Define license
* Define coding conventions
* Define branch strategy
* Define commit convention

### Repository

```text
tallix/
├── apps/
├── services/
├── tests/
├── infra/
├── docs/
└── .github/
```

### Deliverable

A clean repository with documentation and project structure.

---

# 4. Phase 1 — Development Environment

## Goals

Make the project runnable locally.

### Backend

Create:

```text
ASP.NET Core Web API
```

### Frontend

Create:

```text
Next.js
```

### Mobile

Create:

```text
Expo / React Native
```

### Infrastructure

Create Docker Compose for local dependencies.

Initial dependency:

```text
PostgreSQL
```

### Deliverable

A developer should be able to clone the repository and run:

```text
docker compose up
```

and start all required local services.

---

# 5. Phase 2 — Backend Architecture

Implement the modular backend.

```text
Tallix.Api
Tallix.Application
Tallix.Domain
Tallix.Infrastructure
```

Create dependency rules:

```text
API
 ↓
Application
 ↓
Domain

Infrastructure
 ↓
Application / Domain
```

The domain should not depend on infrastructure.

---

# 6. Phase 3 — Database

Create PostgreSQL schema.

Initial entities:

```text
User
Contact
LedgerEntry
Attachment
AuditEntry
```

Create:

* EF Core entities
* DbContext
* Entity configurations
* Migrations
* Development seed data
* Database indexes

---

# 7. Phase 4 — Domain Logic

Implement the ledger domain.

## Transaction Types

```text
LENT
BORROWED
PAYMENT_RECEIVED
PAYMENT_MADE
ADJUSTMENT
```

Implement balance calculation.

Example:

```text
LENT               +5000
PAYMENT_RECEIVED   -1000
LENT               +2000
-----------------------
Balance             6000
```

Create automated unit tests for every calculation rule.

---

# 8. Phase 5 — Authentication

Integrate the chosen identity provider.

Implement:

* Login
* Logout
* Token validation
* User provisioning
* User identity mapping

Protect all user data endpoints.

Test authorization carefully.

Example:

```text
User A
  ↓
GET /api/contacts/user-B-contact
  ↓
403/404
```

---

# 9. Phase 6 — Contact API

Implement:

```text
GET    /api/contacts
POST   /api/contacts
GET    /api/contacts/{id}
PUT    /api/contacts/{id}
DELETE /api/contacts/{id}
```

Requirements:

* Validation
* Ownership checks
* Pagination
* Search
* Tests

---

# 10. Phase 7 — Ledger API

Implement:

```text
GET  /api/contacts/{id}/ledger
POST /api/contacts/{id}/ledger

GET  /api/ledger/{id}

POST /api/ledger/{id}/reverse
```

Support:

* Lending
* Borrowing
* Payments
* Adjustments
* Due dates
* Notes

Add integration tests.

---

# 11. Phase 8 — Dashboard API

Implement:

```text
GET /api/dashboard
```

Return:

```text
totalOwedToMe
totalIOwe
netBalance
overdueCount
upcomingDueCount
recentTransactions
```

Optimize queries only after measuring actual performance.

---

# 12. Phase 9 — Web Application Foundation

Create the Next.js application.

Implement:

* Application layout
* Navigation
* Authentication
* API client
* Error handling
* Loading states
* Empty states
* Responsive design

---

# 13. Phase 10 — Web Dashboard

Build the dashboard.

Example:

```text
------------------------------------
 Tallix
------------------------------------

Owed to me        ₱12,500
I owe             ₱4,300
Net               ₱8,200

------------------------------------

Upcoming
Juan       ₱2,000       Oct 10
Maria      ₱500         Oct 12

------------------------------------

Recent transactions
...
```

---

# 14. Phase 11 — Web Contacts

Implement:

* Contact list
* Search
* Contact creation
* Contact details
* Current balance
* Transaction history

---

# 15. Phase 12 — Web Ledger

Implement:

* Add transaction
* Record payment
* Display transaction history
* Due dates
* Transaction details
* Reversal/correction

The user should be able to complete the entire core workflow from the web application.

---

# 16. Phase 13 — Mobile Foundation

Create the Expo application.

Implement:

* Navigation
* Authentication
* API client
* Secure token handling
* Error states
* Loading states

---

# 17. Phase 14 — Mobile Core Features

Implement:

* Dashboard
* Contact list
* Contact details
* Add transaction
* Record payment
* Transaction history
* Balance display

Prioritize core functionality over feature parity with the web application.

---

# 18. Phase 15 — Testing

Implement testing at multiple levels.

## Unit

Backend:

```text
BalanceCalculatorTests
LedgerEntryTests
TransactionValidationTests
```

Frontend:

```text
Component tests
Utility tests
```

---

## Integration

Test:

```text
API
 +
PostgreSQL
```

Use containerized PostgreSQL where practical.

---

## E2E

Create critical workflows.

### Workflow 1

```text
Login
→ Create contact
→ Lend ₱5,000
→ Verify balance
```

### Workflow 2

```text
Record ₱2,000 payment
→ Verify ₱3,000 remaining
```

### Workflow 3

```text
Create borrowed entry
→ Verify "I owe" dashboard value
```

### Workflow 4

```text
User A
→ Attempt User B resource
→ Access denied
```

---

# 19. Phase 16 — Docker

Create production Dockerfiles.

At minimum:

```text
docker/
├── api.Dockerfile
└── web.Dockerfile
```

Use multi-stage builds.

```text
Build stage
     ↓
Publish stage
     ↓
Small runtime image
```

Scan images for vulnerabilities.

---

# 20. Phase 17 — Azure Infrastructure

Create Terraform configuration.

Provision:

```text
Resource Group
Container Registry
Container Apps
PostgreSQL
Storage Account
Key Vault
Application Insights
Log Analytics
```

Use variables for environment configuration.

---

# 21. Phase 18 — Azure Environments

Create:

```text
Development
Staging
Production
```

Production must not depend on developer machines.

Document all environment-specific configuration.

---

# 22. Phase 19 — CI Pipeline

Create:

```text
.github/workflows/ci.yml
```

Pipeline:

```text
Checkout
   ↓
Install dependencies
   ↓
Lint
   ↓
Build
   ↓
Unit tests
   ↓
Integration tests
   ↓
Security scan
   ↓
Docker build
```

CI should run on:

* Pull requests
* Pushes to main

---

# 23. Phase 20 — CD Pipeline

Create:

```text
.github/workflows/deploy.yml
```

Pipeline:

```text
Build
 ↓
Docker image
 ↓
Azure Container Registry
 ↓
Deploy staging
 ↓
Smoke tests
 ↓
Production approval
 ↓
Deploy production
```

---

# 24. Phase 21 — Database Deployment

Database migrations should be automated carefully.

Development:

```text
dotnet ef database update
```

Production:

```text
Deployment pipeline
        ↓
Migration step
        ↓
Application deployment
```

Migration strategy must avoid destructive changes without explicit review.

---

# 25. Phase 22 — Observability

Implement:

* Structured logging
* Application Insights
* Health checks
* Metrics
* Exception tracking
* Dependency tracking

Create an Azure monitoring dashboard.

Monitor:

```text
Request rate
Error rate
Response time
Availability
Database errors
```

---

# 26. Phase 23 — Security Hardening

Review:

### Authentication

* Token validation
* Expiration
* Secure client storage

### Authorization

* Ownership checks
* Role checks

### Infrastructure

* Key Vault
* Managed identity
* Private database access where appropriate

### Application

* Input validation
* Rate limiting
* Security headers
* CORS
* File upload validation

### Supply Chain

* Dependency scanning
* Container scanning
* Secret scanning

---

# 27. Phase 24 — Attachments

Implement:

```text
POST /api/ledger/{id}/attachments
GET  /api/attachments/{id}
DELETE /api/attachments/{id}
```

Use Azure Blob Storage.

Validate:

* File size
* MIME type
* File extension
* Ownership

Do not expose storage containers publicly.

---

# 28. Phase 25 — Notifications

Add optional reminders.

Examples:

```text
Payment due tomorrow
Payment due today
Payment overdue
```

Start with email.

Push notifications can be added later.

---

# 29. Phase 26 — UX Improvements

Improve:

* Empty states
* Error messages
* Confirmation dialogs
* Mobile responsiveness
* Accessibility
* Keyboard navigation
* Loading indicators
* Optimistic UI where appropriate

---

# 30. Phase 27 — Performance

Measure before optimizing.

Review:

* Database queries
* API response times
* Frontend bundle size
* Mobile performance
* Image/file handling

Add indexes based on actual query patterns.

---

# 31. Phase 28 — Documentation

Document:

```text
README
Architecture
Requirements
Development setup
Deployment
API
Terraform
CI/CD
Security
Troubleshooting
```

Add architecture diagrams.

---

# 32. Phase 29 — Production Release

Before release:

```text
□ Automated tests passing
□ Security scan passing
□ Infrastructure reproducible
□ Database backups enabled
□ Monitoring configured
□ Health checks working
□ Production secrets configured
□ CI/CD working
□ Rollback procedure documented
□ README updated
```

---

# 33. Phase 30 — Portfolio Presentation

The repository should clearly demonstrate:

### Problem

People need a simple way to track informal lending and borrowing.

### Solution

Tallix provides a cross-platform personal ledger.

### Engineering

* TypeScript
* React
* React Native
* C#
* ASP.NET Core
* PostgreSQL

### Cloud

* Azure
* Container Apps
* PostgreSQL
* Blob Storage
* Key Vault
* Application Insights

### DevOps

* Docker
* Terraform
* GitHub Actions
* CI/CD
* Automated testing
* Security scanning

---

# 34. Recommended Git Strategy

Use short-lived feature branches.

```text
main
│
├── feature/contact-management
├── feature/ledger-entries
├── feature/dashboard
├── feature/mobile-ledger
├── feature/azure-infrastructure
└── fix/balance-calculation
```

Pull requests should be required before merging to `main`.

---

# 35. Commit Convention

Use conventional commits.

Examples:

```text
feat: add contact management
feat: add ledger transaction creation
fix: correct payment balance calculation
test: add ledger calculation tests
refactor: simplify ledger service
docs: update deployment guide
ci: add backend test workflow
infra: add postgres terraform module
```

---

# 36. Definition of Done

A feature is complete when:

```text
□ Requirements defined
□ Implementation completed
□ Unit tests added
□ Integration tests added where appropriate
□ UI implemented
□ Error handling implemented
□ Authorization verified
□ Documentation updated
□ Code reviewed
□ CI passing
```

---

# 37. MVP Scope

The first production-capable release should contain only:

```text
Authentication
Contacts
Ledger entries
Payments
Balance calculation
Transaction history
Dashboard
Web application
Mobile application
PostgreSQL
Azure deployment
Docker
Terraform
GitHub Actions
Automated tests
Monitoring
```

Do not delay the MVP for:

```text
Notifications
Attachments
Advanced reporting
PDF generation
Analytics
Recurring transactions
```

Those belong after the core ledger is reliable.

---

# 38. Post-MVP Roadmap

## Release 1.1

* Attachments
* CSV export
* Better search
* Filtering
* Improved dashboard

## Release 1.2

* Email reminders
* Push notifications
* Recurring transactions

## Release 1.3

* Reports
* Monthly summaries
* Charts
* Data export

## Release 2.0

Potential experiments:

* Multi-currency improvements
* Shared ledgers
* Household accounts
* Business mode
* Advanced audit functionality

These should only be added after validating the core product.

---

# 39. Final Target Architecture

```text
                         ┌──────────────────────┐
                         │       GitHub         │
                         │    Source + PRs      │
                         └──────────┬───────────┘
                                    │
                              GitHub Actions
                                    │
                         ┌──────────▼───────────┐
                         │    CI / CD Pipeline  │
                         └──────────┬───────────┘
                                    │
                             Docker Images
                                    │
                         ┌──────────▼───────────┐
                         │ Azure Container      │
                         │ Registry             │
                         └──────────┬───────────┘
                                    │
                    ┌───────────────┴────────────────┐
                    │                                │
             ┌──────▼───────┐                ┌───────▼──────┐
             │ Web Container│                │ API Container │
             │ Next.js      │                │ ASP.NET Core │
             └──────────────┘                └───────┬──────┘
                                                     │
                              ┌──────────────────────┼─────────────────────┐
                              │                      │                     │
                       ┌──────▼──────┐       ┌──────▼──────┐      ┌───────▼───────┐
                       │ PostgreSQL  │       │ Blob Storage│      │   Key Vault   │
                       └─────────────┘       └─────────────┘      └───────────────┘
                                                     │
                                              ┌──────▼──────┐
                                              │ App Insights│
                                              │ Azure Monitor│
                                              └─────────────┘


                 ┌─────────────────────────┐
                 │ React Native / Expo    │
                 │ Mobile Application     │
                 └────────────┬────────────┘
                              │
                              ▼
                         ASP.NET API
```

---

# 40. Success Criteria

Tallix is successful as a portfolio project when a reviewer can:

1. Clone the repository.
2. Understand the architecture from the documentation.
3. Run the project locally.
4. Review clean API/domain/database design.
5. See automated tests.
6. Review Terraform infrastructure.
7. Review GitHub Actions pipelines.
8. See Dockerized applications.
9. Access a deployed Azure environment.
10. Understand monitoring and security decisions.
11. Use both web and mobile clients.
12. Follow a real user workflow from transaction creation to payment and balance calculation.

The primary objective is not to maximize the number of features.

The objective is to demonstrate that a relatively simple product can be engineered using **professional software development, cloud, and DevOps practices**.
