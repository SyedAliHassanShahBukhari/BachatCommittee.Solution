# BachatCommittee Solution

## Overview
BachatCommittee is a modular, PostgreSQL‑backed committee (Bachat / Savings Committee) management system.
The solution is designed using **ASP.NET Core**, **Dapper**, and **FluentMigrator**, with a strong focus on
clean architecture, explicit data access, and database‑first correctness.

This repository serves as the **foundation** for all future development and enforces strict conventions
to avoid long‑term technical debt.

---

## Tech Stack

| Layer | Technology |
|------|------------|
| Backend API | ASP.NET Core |
| ORM / Data Access | Dapper + Dapper.Contrib |
| Database | PostgreSQL |
| Migrations | FluentMigrator |
| CI | GitHub Actions |
| Architecture | Layered / Modular |

---

## Solution Structure

```
BachatCommittee.Solution
│
├── BachatCommittee.API                    # HTTP APIs
├── BachatCommittee.Data.Db                # Base DB utilities & helpers
├── BachatCommittee.Data.Entities          # DB entities (Dapper)
├── BachatCommittee.Data.Repos             # Repositories (explicit, no generic repo)
├── BachatCommittee.Data.Migrations        # FluentMigrator migrations
├── BachatCommittee.Data.Migrator          # Console runner for migrations
├── BachatCommittee.Data.Migration.Test    # Migration validation tests
│
├── BachatCommittee.Models.Enums
├── BachatCommittee.Models.Classes
├── BachatCommittee.Models.DTOs.Requests
├── BachatCommittee.Models.DTOs.Responses
│
├── BachatCommittee.Data.Mappers           # Mapping logic
├── BachatCommittee.ServiceCollection      # Dependency injection wiring
│
└── .github/workflows                      # CI pipelines
```

---

## Database & Migration Strategy

### PostgreSQL
- UUIDs are used as **primary keys**
- Required extensions:
  - `pgcrypto`
  - `uuid-ossp`

### FluentMigrator Rules
- Every migration:
  - Has a fixed epoch‑based migration ID
  - Is **idempotent**
  - Uses explicit column definitions
- No EF Core migrations are used — **FluentMigrator only**

Run migrations locally:

```bash
dotnet run --project BachatCommittee.Data.Migrator
```

---

## Dapper Standards

- No Generic Repository pattern
- Each entity has its own repository
- All queries:
  - Use `CommandType.Text`
  - Are explicit and readable
- Table names are globally quoted to support PostgreSQL casing

---

## CI Pipeline

GitHub Actions workflow:
- Restores packages
- Builds solution
- Runs tests
- Spins up PostgreSQL service
- (Optionally) runs migrations

Branch used: **master**

---

## Domain Roadmap (from SRS)

Planned core modules:
1. Committee Management
2. Member Management
3. Monthly Contributions
4. Draw / Payout Logic
5. Ledger & Audit Trail
6. Roles & Permissions (future)

Each module will include:
- Migration
- Entity
- Repository
- DTOs
- Service layer
- API endpoints

---

## Contribution Rules

- Follow existing naming conventions
- No breaking schema changes without migration
- No implicit magic (everything explicit)
- No EF Core for data access
- Keep repositories thin and readable

---

## Status

**Current State:** Foundation complete  
**Next Step:** Implement first domain module (Committee)

---

## License
MIT
