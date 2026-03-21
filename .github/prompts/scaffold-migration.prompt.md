---
description: "Creates an EF Core migration for Petatu.Infrastructure with safety checklist. Generates the migration command and reviews the planned schema changes."
agent: "agent"
argument-hint: "Migration name describing the schema change (e.g., 'AddPetBreedId', 'CreateDietEntriesTable')"
tools: ["read", "search", "edit", "execute"]
---

Create an EF Core database migration for Petatu. Migration name: **$ARGUMENTS**

Follow the instructions in [efcore.instructions.md](../instructions/efcore.instructions.md).

## Pre-Migration Checklist

Before generating the migration, verify:

1. **Read the entity and configuration** — confirm the EF model changes are complete:
   - Entity class in `API/src/Petatu.Domain/Entities/`
   - `IEntityTypeConfiguration<T>` in `API/src/Petatu.Infrastructure/Data/Configurations/`
   - `DbSet<T>` added to `ApplicationDbContext`

2. **Check existing migrations** — read the latest migration in `API/src/Petatu.Infrastructure/Data/Migrations/` to understand the current schema state.

3. **Naming check** — the migration name must:
   - Be PascalCase
   - Describe what changes (e.g., `AddPetBreedIdColumn`, `CreateWeightEntriesTable`)
   - Not be generic like `UpdateModel` or `Migration1`

## Generate the Migration

Run from the solution root:

```bash
dotnet ef migrations add $ARGUMENTS \
  --project API/src/Petatu.Infrastructure \
  --startup-project API/src/Petatu.Web
```

## Post-Generation Review

After the migration is generated, read the new migration file and verify:

- [ ] `Up()` applies the intended changes only — no unexpected drops or renames
- [ ] `Down()` correctly reverses the `Up()` migration
- [ ] No `DROP COLUMN` unless intentionally removing a column that is no longer written to
- [ ] No implicit column rename (EF generates DROP + ADD) — if renaming, use `migrationBuilder.RenameColumn()` instead
- [ ] Column types are correct for PostgreSQL (e.g., `text` for strings, `uuid` for Guid, `timestamp with time zone` for `DateTimeOffset`)
- [ ] snake_case naming applied to all new columns and tables

Report any issues found in the review before proceeding.
