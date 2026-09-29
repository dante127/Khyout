# Khyout — B2B Marketplace for the Syrian Textile Supply Chain

Khyout connects garment workshop owners (buyers) with raw fabric/yarn suppliers
(importers, spinning mills): a technical fabric catalog (GSM, composition, weave),
a time-bound RFQ workflow, swatch ordering, and Telegram-based notifications —
designed for low-bandwidth, intermittent-connectivity environments.

The full architecture specification lives in
`docs/architecture/khyout-phase1-spec-2026-09-29.md` (Phase 1 deliverable).
Phases 1–3 are implemented and tested. Phase 4 is in progress: 4a (media
pipeline, Telegram integration, deployment scaffolding) is done, and the
Phase 4b-1 offline-first PWA client scaffold now lives in `client/`.

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
├─ client/                    # offline-first PWA client (Vite + React + TS + Tailwind, RTL)
└─ deploy/                    # PostgreSQL DDL scripts + Docker Compose deployment stack
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

## Running the API locally

```sh
dotnet run --project src/Khyout.Api
```

- Development defaults: SQLite (`khyout.dev.db`, created automatically via EnsureCreated) and a dev JWT signing key.
- OTP codes are delivered by `LoggingSmsSender` in development — the 6-digit code appears in the console log.
- Swagger UI (Development only): `http://localhost:<port>/swagger`.
- For anything beyond local development, set `Auth:Jwt:SigningKey` and `ConnectionStrings:Postgres` (env or appsettings).
- Background workers (quote/RFQ expiry, outbox dispatcher) respect `BackgroundJobs:Enabled` (default on).

## Web client (Phase 4b)

The PWA client lives in `client/` — Vite + React + TypeScript + Tailwind CSS,
Arabic RTL, offline-first (IndexedDB outbox scaffold + refresh-token rotation).

```sh
# terminal 1 — run the API on the port the client dev proxy expects
dotnet run --project src/Khyout.Api --urls http://localhost:5000

# terminal 2
cd client
npm install
npm run dev   # http://localhost:5173 — /api is proxied to :5000
```

See `client/README.md` for env vars, scripts, and architecture notes.

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

## Admin bootstrap (Phase 3)

Admin users are intentionally not seeded through migrations. At startup, the first
user whose phone number matches `AdminBootstrap:PhoneNumber` (configuration/env)
after a normal OTP login is promoted to `Admin`.

## Roadmap

| Phase | Scope | Status |
|---|---|---|
| 1 | Architecture & technical specification | done |
| 2 | Domain model, EF Core configuration, migrations, DDL | done |
| 3 | Application layer (CQRS), REST API, background workers | done |
| 4 | Telegram integration, offline-first PWA, deployment | in progress — 4a done; 4b-1 scaffold, 4b-2a catalog + 4b-2b RFQ flow done |
