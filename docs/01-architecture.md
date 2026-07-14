# 01 — Architecture

## System overview

CycleSync is a classic SPA + API + relational database, orchestrated in development by
.NET Aspire. External capabilities (maps, LLM intelligence) sit behind server-side
abstractions so the SPA never holds third-party secrets.

```
                         ┌──────────────────────────────────────────┐
                         │              .NET Aspire AppHost           │
                         │  (orchestration, service discovery, otel)  │
                         └──────────────────────────────────────────┘
                            │            │              │          │
        ┌───────────────────┘            │              │          └───────────────┐
        ▼                                ▼              ▼                          ▼
┌───────────────┐   HTTPS/JSON   ┌────────────────┐  ┌──────────────┐    ┌──────────────────┐
│  React SPA    │ ─────────────▶ │  CycleSync.Api │  │ SQL Server   │    │ MigrationService │
│ (Vite dev /   │ ◀───────────── │ (ASP.NET Core) │─▶│  (container) │◀───│ (EF migrations)  │
│  static host) │                │  Minimal APIs  │  └──────────────┘    └──────────────────┘
└───────────────┘                └────────────────┘
                                        │  server-side integrations (secrets stay here)
                                        ├──────────────▶ Azure Maps (geocode / search)
                                        ├──────────────▶ LLM provider (location intelligence)
                                        └──────────────▶ Google OIDC (token validation)
```

## Aspire topology

The `CycleSync.AppHost` project declares the resource graph:

- **`sqlserver`** — SQL Server container resource with a persistent data volume; exposes a
  `cyclesync` database. Connection string injected into the API and migration service.
- **`migrations`** (`CycleSync.MigrationService`) — a worker that applies EF Core migrations
  on startup, then reports healthy. The API waits for it (`WaitForCompletion`).
- **`api`** (`CycleSync.Api`) — references `sqlserver`; receives config via environment
  variables (connection string, Google client id/secret, Azure Maps key, LLM key).
- **`web`** — the React app run through Vite in development, or served as static files in
  production. The API base URL is injected as an environment variable; no service-discovery
  client leaks into the browser bundle.

Config is emitted explicitly by the AppHost as environment variables (per the
`aspire-configuration` guidance) — app code contains no Aspire client packages or
service-discovery calls.

## ServiceDefaults

A shared `CycleSync.ServiceDefaults` project centralises OpenTelemetry (traces, metrics,
logs), health checks (`/health`, `/alive`), HTTP resilience handlers, and service discovery
for server-to-server calls. Both `Api` and `MigrationService` call `AddServiceDefaults()`.

## Backend architecture

- **ASP.NET Core Minimal APIs**, organised as **vertical slices** (one folder per feature:
  endpoint + request/response DTOs + handler). Keeps each BDD feature's code co-located.
- **EF Core 10** for persistence. Reads use `AsNoTracking` by default; navigation-collection
  queries use split queries. Write and read models are separated where it helps
  (see `database-performance` guidance).
- **Domain layer** holds entities, value objects, and the attendance state machine as pure
  C# (records + pattern matching), independent of EF and ASP.NET.
- **Integration abstractions**: `IMapsSearch`, `ILocationIntelligence`, `ICostEstimator`.
  Real implementations call Azure Maps / the LLM; test doubles are swapped in BDD runs via
  configuration so scenarios are deterministic and offline.

## Authentication & authorization

- **Google OIDC**. The SPA performs the authorization-code + PKCE flow; the API validates
  the ID token, enforces the allowed domain (`hd` claim / email-domain allow-list), and
  issues a short-lived app session (HTTP-only cookie or JWT).
- **Authorization is flat**: a single authenticated policy. No roles, no admin — enforcing
  the "equal access" principle. Every protected endpoint requires the authenticated policy.

## Cross-cutting concerns

| Concern | Approach |
| --- | --- |
| Telemetry | OpenTelemetry via ServiceDefaults; viewed in the Aspire dashboard |
| Errors | RFC 7807 `ProblemDetails`; typed error taxonomy (validation, not-found, forbidden, upstream) |
| Resilience | Standard resilience handler on outbound HTTP (maps, LLM) |
| Secrets | User Secrets in dev, environment/Key Vault in prod; never shipped to the browser |
| Caching | Location intelligence and cost estimates cached with generation timestamp + confidence |
| Time | `TimeProvider` injected everywhere for deterministic tests |

## Environments

- **Development**: `dotnet run` on the AppHost boots everything; SQL Server in a container.
- **CI**: Aspire test host + Testcontainers SQL Server for integration/BDD; Playwright for UI.
- **Production**: containerised API + static SPA behind a reverse proxy; managed SQL Server.

See [07-solution-structure.md](07-solution-structure.md) for the concrete project layout.
