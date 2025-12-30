# Architecture

## Goals
- Maintainable, explicit codebase
- Stable database evolution via migrations
- Clean separation between API, domain models, data access, and infrastructure
- PostgreSQL-first correctness (UUIDs, quoting, extension management)

## High-Level Design
The system follows a layered structure:

1. **API Layer** (`BachatCommittee.API`)
   - Controllers / endpoints
   - Input validation
   - DTO mapping
   - Returns standardized API responses

2. **Services / Composition** (`BachatCommittee.ServiceCollection`)
   - Dependency injection registration
   - Cross-cutting policies

3. **Data Access**
   - **Entities** (`BachatCommittee.Data.Entities`): Dapper entity models
   - **Repositories** (`BachatCommittee.Data.Repos`): Explicit repositories with SQL
   - **DB Utilities** (`BachatCommittee.Data.Db`): connection factory, helpers, quoting rules

4. **Migrations**
   - **Migrations** (`BachatCommittee.Data.Migrations`): FluentMigrator migrations
   - **Migrator Runner** (`BachatCommittee.Data.Migrator`): console runner for applying migrations
   - **Migration Tests** (`BachatCommittee.Data.Migration.Test`): ensures migrations are valid

## Database Conventions
- PostgreSQL is the source of truth
- Primary keys: UUID
- Required extensions:
  - `pgcrypto`
  - `uuid-ossp`
- Table and column naming:
  - Use consistent casing
  - Dapper.Contrib is configured to quote identifiers to avoid casing conflicts

## Repository Conventions
- No generic repositories
- Each repository:
  - encapsulates SQL for one entity aggregate
  - uses `CommandType.Text`
  - remains read-focused and testable

## DTO Conventions
- Requests and responses are separate projects/namespaces:
  - `BachatCommittee.Models.DTOs.Requests`
  - `BachatCommittee.Models.DTOs.Responses`
- Entities never cross API boundaries.

## CI/CD
- GitHub Actions builds and tests on push/PR to `master`
- PostgreSQL service container is used for integration-oriented checks
- Optional: run migrator as part of CI for schema validation

## Module Implementation Pattern
For each new module (e.g., Committee, Member, Contribution):
1. Migration(s)
2. Entity/Entities
3. Repository
4. DTOs (Request/Response)
5. Service layer (if needed)
6. DI registration
7. API endpoints
8. Tests (unit/integration where feasible)
