# Khyout — B2B Marketplace for the Syrian Textile Supply Chain

Khyout connects garment workshop owners (buyers) with raw fabric/yarn suppliers
(importers, spinning mills): a technical fabric catalog (GSM, composition, weave),
a time-bound RFQ workflow, swatch ordering, and Telegram-based notifications —
designed for low-bandwidth, intermittent-connectivity environments.

The full architecture specification lives in
`docs/architecture/khyout-phase1-spec-2026-09-29.md` (Phase 1 deliverable).
This repository currently implements **Phase 2 — Domain Modeling & Database Setup**.

## Solution layout

```
Khyout.sln
├─ src/
│  ├─ Khyout.Domain/          # entities, enums, reference data — no dependencies
│  ├─ Khyout.Application/     # use cases (CQRS) — scaffolded in Phase 2, filled in Phase 3
│  ├─ Khyout.Infrastructure/  # EF Core (PostgreSQL/SQLite), migrations
│  └─ Khyout.Api/             # minimal host in Phase 2; REST API in Phase 3
├─ tests/
│  ├─ Khyout.Domain.Tests/    # domain invariant tests
│  └─ Khyout.Api.IntegrationTests/
└─ deploy/sql/                # generated, idempotent PostgreSQL DDL scripts
```

## Requirements

- .NET SDK **10.0.101** (pinned via `global.json`; newer 10.0.x features roll forward)
- PostgreSQL 16 for production; SQLite for local development
- Node.js is only needed from Phase 4 onward (PWA client)

## Build & test

```sh
dotnet build -c Release
dotnet test
```

## Database

Provider selection is in `src/Khyout.Infrastructure/DependencyInjection.cs`:

- **Default:** PostgreSQL — configure `ConnectionStrings:Postgres`.
- **Local dev:** set `Database:UseSqlite=true` — optional `ConnectionStrings:Sqlite`
  (defaults to `Data Source=khyout.dev.db`).

Migrations are PostgreSQL-only; local SQLite usage relies on `EnsureCreated()`.

```sh
dotnet tool restore

# create/update schema on PostgreSQL
dotnet ef migrations add <Name> -p src/Khyout.Infrastructure -s src/Khyout.Api
dotnet ef database update -p src/Khyout.Infrastructure -s src/Khyout.Api

# regenerate the idempotent DDL script (run from the repository root)
dotnet ef migrations script --idempotent -p src/Khyout.Infrastructure -s src/Khyout.Api -o deploy/sql/001_initial_schema.sql
```

The initial schema script is checked in at `deploy/sql/001_initial_schema.sql`.

## Admin bootstrap (planned — Phase 3)

Admin users are intentionally not seeded through migrations. Plan: after a normal
OTP login, the first user matching `AdminBootstrap:PhoneNumber` (configuration/env)
is promoted to `Admin` on startup.

## Roadmap

| Phase | Scope | Status |
|---|---|---|
| 1 | Architecture & technical specification | done |
| 2 | Domain model, EF Core configuration, migrations, DDL | done |
| 3 | Application layer (CQRS) & REST API | next |
| 4 | Telegram integration, offline-first PWA, deployment | planned |
