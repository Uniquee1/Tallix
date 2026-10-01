# Tallix

**Tallix** is a simple personal lending and borrowing ledger that helps users keep track of who owes them and who they owe.

It provides a web application and mobile application backed by a shared API and cloud infrastructure.

Tallix is designed as a portfolio-quality software engineering project demonstrating modern full-stack development, mobile development, Azure cloud infrastructure, DevOps, automated testing, Infrastructure as Code, security, and observability.

> Tallix is a personal record-keeping application. It is not intended to function as a regulated lending, banking, accounting, or financial institution platform.

---

## Features

### Personal Ledger

- Record money lent to someone
- Record money borrowed from someone
- Record partial payments
- Track outstanding balances
- View complete transaction history
- Add notes and descriptions
- Set optional due dates
- Correct or reverse transactions

### Contacts

- Create and manage contacts
- View all transactions associated with a contact
- See whether a contact owes you or you owe them
- View current outstanding balance

### Dashboard

The dashboard provides a quick overview of:

- Total owed to you
- Total you owe
- Net balance
- Overdue entries
- Upcoming due dates
- Recent transactions

### Authentication

- Secure user authentication
- User-specific data isolation
- API authorization
- External identity provider integration

### Web Application

- Responsive web interface
- Dashboard
- Contact management
- Ledger management
- Transaction history
- Search and filtering

### Mobile Application

- Android/iOS support
- Shared backend API
- Mobile-friendly ledger management
- Transaction recording
- Balance and history views
- Push notifications in a future release

---

# Technology Stack

## Web

- Next.js
- React
- TypeScript
- Tailwind CSS
- TanStack Query
- Playwright

## Mobile

- React Native
- Expo
- TypeScript

## Backend

- ASP.NET Core Web API
- C#
- Entity Framework Core
- PostgreSQL
- OpenAPI/Swagger

## Cloud

- Microsoft Azure
- Azure Container Apps
- Azure Database for PostgreSQL
- Azure Blob Storage
- Azure Key Vault
- Azure Container Registry
- Azure Application Insights
- Azure Monitor

## DevOps

- GitHub
- GitHub Actions
- Docker
- Terraform
- Automated testing
- Dependency/security scanning
- CI/CD
- Infrastructure as Code

---

# Architecture

Tallix uses a **modular monolith** architecture.

```text
                    â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
                    â”‚     Web Client      â”‚
                    â”‚ Next.js / React / TSâ”‚
                    â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
                               â”‚
                               â”‚ HTTPS
                               â”‚
                    â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â–¼â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
                    â”‚     ASP.NET Core    â”‚
                    â”‚      Web API        â”‚
                    â”‚                     â”‚
                    â”‚  Modular Monolith   â”‚
                    â””â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”˜
                           â”‚     â”‚
                â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜     â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
                â–¼                           â–¼
       â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”         â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
       â”‚   PostgreSQL    â”‚         â”‚ Azure Blob      â”‚
       â”‚                 â”‚         â”‚ Storage         â”‚
       â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜         â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜

                    â–²
                    â”‚ HTTPS
                    â”‚
          â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
          â”‚ React Native / Expoâ”‚
          â”‚    Mobile App      â”‚
          â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜

```

Infrastructure and deployment are managed through Terraform and GitHub Actions.

---

# Repository Structure

```text
tallix/
â”‚
â”œâ”€â”€ apps/
â”‚   â”œâ”€â”€ web/
â”‚   â”‚   â””â”€â”€ Next.js application
â”‚   â”‚
â”‚   â””â”€â”€ mobile/
â”‚       â””â”€â”€ React Native / Expo application
â”‚
â”œâ”€â”€ services/
â”‚   â””â”€â”€ api/
â”‚       â”œâ”€â”€ Tallix.Api/
â”‚       â”œâ”€â”€ Tallix.Application/
â”‚       â”œâ”€â”€ Tallix.Domain/
â”‚       â””â”€â”€ Tallix.Infrastructure/
â”‚
â”œâ”€â”€ tests/
â”‚   â”œâ”€â”€ unit/
â”‚   â”œâ”€â”€ integration/
â”‚   â””â”€â”€ e2e/
â”‚
â”œâ”€â”€ infra/
â”‚   â””â”€â”€ terraform/
â”‚
â”œâ”€â”€ docs/
â”‚   â”œâ”€â”€ architecture.md
â”‚   â”œâ”€â”€ requirements.md
â”‚   â””â”€â”€ plan.md
â”‚
â”œâ”€â”€ .github/
â”‚   â””â”€â”€ workflows/
â”‚
â”œâ”€â”€ docker/
â”‚
â”œâ”€â”€ README.md
â”œâ”€â”€ requirements.md
â”œâ”€â”€ architecture.md
â””â”€â”€ plan.md

```

---

# Core Domain Concept

Tallix is based on a transaction ledger rather than a manually maintained balance.

For example:

```text
Juan Santos

Oct 01    Lent              +â‚±5,000
Oct 05    Payment Received  -â‚±1,000
Oct 10    Lent              +â‚±2,000
-------------------------------------
Balance                      â‚±6,000

```

The balance is derived from ledger transactions.

This provides:

- Auditability
- Transaction history
- Easier corrections
- Partial payments
- Multiple transactions with the same person
- Reduced risk of inconsistent balances

---

# Transaction Types

The initial transaction types are:

```text
LENT
BORROWED
PAYMENT_RECEIVED
PAYMENT_MADE
ADJUSTMENT

```

The transaction direction is interpreted relative to the current user.

```text
LENT
    â†’ increases amount owed to the user

PAYMENT_RECEIVED
    â†’ decreases amount owed to the user

BORROWED
    â†’ increases amount owed by the user

PAYMENT_MADE
    â†’ decreases amount owed by the user

```

---

# Local Development

## Prerequisites

Install:

- Git
- .NET SDK
- Node.js
- Docker
- Docker Compose
- PostgreSQL client (optional)
- Expo development tooling

Terraform is required for infrastructure work.

---

## Clone

```bash
git clone <repository-url>
cd tallix

```

---

## Start Local Infrastructure

```bash
docker compose up -d

```

This starts local dependencies such as PostgreSQL.

---

## Run API

```bash
cd services/api
dotnet restore
dotnet run

```

---

## Run Web Application

```bash
cd apps/web
npm install
npm run dev

```

---

## Run Mobile Application

```bash
cd apps/mobile
npm install
npx expo start

```

---

# Testing

Run backend tests:

```bash
dotnet test

```

Run frontend tests:

```bash
npm test

```

Run end-to-end tests:

```bash
npx playwright test

```

---

# Development Workflow

Tallix follows a pull-request-based workflow.

```text
feature/*
     â”‚
     â–¼
Pull Request
     â”‚
     â”œâ”€â”€ Build
     â”œâ”€â”€ Unit tests
     â”œâ”€â”€ Integration tests
     â”œâ”€â”€ Lint
     â”œâ”€â”€ Security checks
     â””â”€â”€ Docker build
     â”‚
     â–¼
Code Review
     â”‚
     â–¼
main
     â”‚
     â–¼
CI/CD
     â”‚
     â–¼
Staging
     â”‚
     â–¼
Production

```

---

# Infrastructure

Infrastructure is defined using Terraform.

Major Azure resources include:

- Resource Group
- Azure Container Apps
- Azure Container Registry
- Azure Database for PostgreSQL
- Azure Blob Storage
- Azure Key Vault
- Application Insights
- Log Analytics

The goal is for the environment to be reproducible rather than manually configured through the Azure Portal.

---

# Security Principles

Tallix follows these principles:

- No secrets committed to Git
- HTTPS-only communication
- External identity provider for authentication
- Authorization at API boundaries
- Per-user data isolation
- Private storage for uploaded files
- Managed identities where possible
- Secrets stored in Azure Key Vault
- Input validation
- Database parameterization through EF Core
- Dependency vulnerability scanning
- Audit-friendly transaction model

---

# Observability

Production services should provide:

- Application logs
- Request metrics
- Error tracking
- Dependency tracking
- Response-time metrics
- Availability monitoring

Azure Application Insights and Azure Monitor are used for centralized observability.

---

# Project Goals

Tallix is intended to demonstrate practical experience with:

- Full-stack application development
- REST API design
- Relational database design
- Domain modeling
- React/Next.js
- React Native
- C#
- ASP.NET Core
- PostgreSQL
- Docker
- Azure
- Terraform
- GitHub Actions
- Automated testing
- CI/CD
- Security
- Monitoring and observability

---

# Non-Goals

The following are intentionally outside the initial scope:

- Banking
- Credit scoring
- Loan underwriting
- Automated loan approval
- Payment processing
- Bank account integration
- Cryptocurrency
- Complex accounting
- Multi-institution lending
- Regulated financial services

These may be considered only as future experiments.

---

# License

License: TBD
