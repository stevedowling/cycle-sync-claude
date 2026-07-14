# 07 — Solution structure

Modern .NET layout (`.slnx`, Central Package Management, `Directory.Build.props`, pinned SDK
via `global.json`). React app lives beside the backend and is orchestrated by Aspire.

## Repository layout

```
cycle-sync/
├── global.json                     # Pinned .NET SDK
├── Directory.Build.props           # Shared build settings (nullable, analyzers, langversion)
├── Directory.Packages.props        # Central Package Management (versions here only)
├── CycleSync.slnx                  # Solution
├── PLAN.md
├── docs/                           # These design docs
│
├── src/
│   ├── CycleSync.AppHost/          # Aspire orchestration (sqlserver, api, web, migrations)
│   ├── CycleSync.ServiceDefaults/  # OTel, health checks, resilience, discovery
│   ├── CycleSync.Api/              # ASP.NET Core Minimal APIs (vertical slices)
│   │   ├── Features/               #   one folder per feature (endpoint + DTOs + handler)
│   │   │   ├── Auth/
│   │   │   ├── Profile/
│   │   │   ├── Locations/
│   │   │   ├── Interests/
│   │   │   ├── OffCycles/
│   │   │   └── Attendance/
│   │   └── Program.cs
│   ├── CycleSync.Domain/           # Entities, value objects, AttendanceStatus state machine
│   ├── CycleSync.Infrastructure/   # EF Core DbContext, migrations, external integrations
│   │   ├── Persistence/            #   DbContext, configurations
│   │   ├── Maps/                   #   IMapsSearch + Azure Maps impl
│   │   ├── Intelligence/           #   ILocationIntelligence + LLM impl
│   │   └── Costing/                #   ICostEstimator + heuristic impl
│   ├── CycleSync.MigrationService/ # Applies EF migrations on startup, then healthy
│   └── web/                        # React + TypeScript + Vite SPA
│       ├── src/
│       │   ├── app/                #   store, router, api slice
│       │   ├── features/           #   mirrors backend feature areas
│       │   └── components/
│       ├── package.json
│       └── vite.config.ts
│
└── tests/
    ├── features/                   # Gherkin .feature files (the BDD specs)
    ├── CycleSync.Acceptance/       # Reqnroll step defs, fixtures, test doubles
    │   ├── Steps/
    │   ├── Support/                #   SystemFixture, Testcontainers, fakes
    │   └── CycleSync.Acceptance.csproj  # references tests/features/*.feature
    ├── CycleSync.Domain.Tests/     # xUnit unit tests (invariants, state machine, heuristics)
    └── web/                        # Vitest component tests (co-located or here)
```

> The `.feature` files live in `tests/features/` and are linked into the
> `CycleSync.Acceptance` project so a single canonical copy is both documentation and test
> input.

## Project references

```
AppHost ─────▶ Api, MigrationService, ServiceDefaults, web (as resource)
Api ─────────▶ Domain, Infrastructure, ServiceDefaults
MigrationService ▶ Infrastructure, ServiceDefaults
Infrastructure ▶ Domain
Acceptance ──▶ Api (via test host), Infrastructure (fakes)
Domain.Tests ▶ Domain
```

Domain has **no** dependencies on EF, ASP.NET, or Aspire — keeps invariants unit-testable.

## Tooling & conventions

| Area | Choice |
| --- | --- |
| SDK pinning | `global.json` |
| Packages | Central Package Management (`Directory.Packages.props`); use `dotnet add`, never hand-edit versions |
| Language | C# latest; nullable enabled; analyzers on; records + pattern matching |
| EF Core | NoTracking by default, split queries, migrations applied by MigrationService |
| API style | Minimal APIs, vertical slices, `ProblemDetails` errors |
| Frontend | TypeScript strict, RTK Query, ESLint + Prettier |
| Local tools | `dotnet-tools.json` (ef, reqnroll if needed) |
| CI | build → unit → Reqnroll (Testcontainers) → Playwright → frontend checks |

## Getting started (once implemented)

```bash
# Backend + SQL + React, all orchestrated:
dotnet run --project src/CycleSync.AppHost

# Acceptance (BDD) suite:
dotnet test tests/CycleSync.Acceptance

# Frontend only:
cd src/web && npm install && npm run dev
```

## Build order (bootstrapping Phase 0)

1. Solution scaffold: `.slnx`, `global.json`, `Directory.*.props`.
2. `ServiceDefaults`, then `AppHost` with a `sqlserver` resource.
3. `Domain` (empty), `Infrastructure` (DbContext + first migration), `MigrationService`.
4. `Api` with `/health` and one `GET /api/ping`.
5. `web` Vite app shell calling `/api/ping`.
6. `CycleSync.Acceptance` with the Reqnroll harness + `smoke.feature` green.
