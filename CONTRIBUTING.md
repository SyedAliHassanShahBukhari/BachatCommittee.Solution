# Contributing

## Branching
- Default branch: `master`
- Use short-lived feature branches:
  - `feature/<topic>`
  - `fix/<topic>`
  - `chore/<topic>`

## Commit Messages
Use clear, imperative messages:
- `Add committee table migration`
- `Fix member repo null handling`
- `Update CI to run migrator`

## Coding Standards (Non-Negotiable)
### Data Access
- **Dapper/Dapper.Contrib only** for persistence (no EF Core for runtime data access).
- No generic repository pattern. Repositories must be explicit and entity-specific.
- SQL should be readable, explicit, and tested where practical.

### FluentMigrator
- All schema changes must be done via migrations (no manual DB changes).
- Migrations must be deterministic and environment-safe.
- Prefer UUID primary keys.
- When seeding reference data, prefer **fixed UUIDs** to keep environments consistent.

### DTOs & API Contracts
- Use DTOs for request/response; do not expose entities from controllers.
- Keep request and response models separate.

### Logging & Error Handling
- Log meaningful events and errors.
- Prefer structured logging.
- No silent catches.

## Pull Requests
Before opening a PR:
- Run locally:
  ```bash
  dotnet restore
  dotnet build -c Release
  dotnet test -c Release
  ```
- If your change impacts schema:
  - Add/Update migration(s)
  - Verify migrator runs successfully against a fresh PostgreSQL database

## Security
- Do not commit secrets.
- Use user-secrets or environment variables for local dev.
- Keep connection strings out of committed appsettings where possible.

## Help
If you are unsure about a convention, open a discussion or issue before implementing a divergent pattern.
