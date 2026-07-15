# CycleSync — Build Plan

> A single-page application that helps globally distributed teams decide where to meet by
> aggregating location preferences, providing travel intelligence, and facilitating
> off-cycle event planning.

This plan describes how to build CycleSync as a **React SPA** backed by a **C# / .NET Web API**,
persisted in **SQL Server**, and orchestrated locally with **.NET Aspire**. Development is
**BDD-first**: every feature begins as an executable Gherkin specification that fails, then we
implement until it passes.

## Document map

| Document | Purpose |
| --- | --- |
| [PLAN.md](PLAN.md) (this file) | Roadmap, phases, and definition of done |
| [docs/01-architecture.md](docs/01-architecture.md) | System architecture, Aspire topology, cross-cutting concerns |
| [docs/02-domain-model.md](docs/02-domain-model.md) | Entities, value objects, invariants, state machines |
| [docs/03-database-schema.md](docs/03-database-schema.md) | SQL Server schema, EF Core mapping, migrations |
| [docs/04-api-design.md](docs/04-api-design.md) | REST endpoints, contracts, auth, error model |
| [docs/05-frontend-spa.md](docs/05-frontend-spa.md) | React app structure, routing, state, screens |
| [docs/06-bdd-strategy.md](docs/06-bdd-strategy.md) | Test pyramid, Reqnroll + Playwright, CI gates |
| [docs/07-solution-structure.md](docs/07-solution-structure.md) | Projects, folders, naming, tooling |
| [docs/08-phase-0-walking-skeleton.md](docs/08-phase-0-walking-skeleton.md) | Phase 0 (built): what exists, how to run, limitations |
| [docs/09-phase-1-auth-profiles.md](docs/09-phase-1-auth-profiles.md) | Phase 1 (built): auth, profiles, persistence, testing approach |
| [docs/10-phase-2-locations-discovery.md](docs/10-phase-2-locations-discovery.md) | Phase 2 (built): location search, persistence, AI intelligence |
| [docs/11-phase-4-offcycles-attendance.md](docs/11-phase-4-offcycles-attendance.md) | Phase 4 (built): off-cycles, attendance, cost estimation |
| [tests/features/](tests/features/) | Executable Gherkin feature files (the BDD specs) |

## Guiding principles (from the brief)

1. **Equal access** — all authenticated users have identical rights; there are no admins.
2. **Transparent costs** — always surface estimate confidence and generation timestamps.
3. **Privacy-friendly** — all data is visible to all authenticated users in the workspace.
4. **Location permanence** — locations are never deleted (soft-delete/archival only if ever).

These are non-functional acceptance criteria and appear as scenarios in the BDD suite.

## Tech stack

| Layer | Choice | Notes |
| --- | --- | --- |
| Orchestration | .NET Aspire (AppHost) | Wires API, SQL Server, React dev server, telemetry |
| Backend | ASP.NET Core (.NET 10) Minimal APIs | Vertical-slice endpoints, EF Core |
| Data | SQL Server 2022 | EF Core 10, code-first migrations |
| Frontend | React 18 + TypeScript + Vite | RTK Query for data, React Router |
| Auth | Google OAuth (OIDC), domain-restricted | `hd` claim / allow-list enforced server-side |
| External | Azure Maps (geocode/search), LLM (location intelligence) | Behind server-side abstractions |
| BDD | Reqnroll (SpecFlow successor) + Playwright | Gherkin drives API + UI acceptance tests |
| Unit/Integration | xUnit + Aspire testing + Testcontainers | Real SQL Server in a container for integration |

## BDD-first workflow

Every feature follows the same loop:

1. **Specify** — write/expand a `.feature` file in [tests/features/](tests/features/) as
   Gherkin. Review it with stakeholders as living documentation.
2. **Red** — implement step definitions that call the real system; the scenario fails.
3. **Green** — build the vertical slice (endpoint → domain → EF → migration → React screen)
   until the scenario passes.
4. **Refactor** — clean up with the suite green.
5. **Guard** — the feature file stays in CI as a regression gate.

See [docs/06-bdd-strategy.md](docs/06-bdd-strategy.md) for the full testing approach.

## Delivery phases

Each phase is a thin, demoable vertical slice. A phase is **done** only when its feature files
are green in CI and the slice is reachable in the running Aspire app.

### Phase 0 — Walking skeleton ✅ built
- Aspire AppHost boots API + SQL Server + React (Vite) app.
- Health endpoint returns `200`; React shell renders and calls `/api/ping`.
- Reqnroll harness runs `smoke.feature` green (2/2). Playwright wiring deferred to Phase 1.
- **Features:** `smoke.feature` · **Details:** [docs/08-phase-0-walking-skeleton.md](docs/08-phase-0-walking-skeleton.md)

### Phase 1 — Authentication & profiles ✅ built
- Google sign-in with domain restriction enforced server-side; provisioning on first sign-in.
- Application session JWT issuance; unauthenticated calls rejected (`401`).
- User profile CRUD: home location, currency, language, passports.
- EF Core + SQL Server, `InitialCreate` migration, `CycleSync.MigrationService`.
- Reqnroll suite green (11/11). **Details:** [docs/09-phase-1-auth-profiles.md](docs/09-phase-1-auth-profiles.md)
- **Features:** `authentication.feature`, `user-profile.feature`

### Phase 2 — Locations & discovery ✅ built
- Azure Maps-backed location search (proxied server-side behind `IMapsSearch`).
- Location persistence (permanent, de-duplicated); AI-generated intelligence with timestamp +
  confidence, cached and regenerated when stale; passport-aware visa guidance.
- React SPA screens (sign-in, search/persist, intelligence detail) wired via RTK Query, with
  Vitest + Testing Library component tests.
- Reqnroll suite green (19/19); Playwright full-stack E2E and a real-SQL-Server fidelity pass run in
  GitHub Actions CI. **Details:** [docs/10-phase-2-locations-discovery.md](docs/10-phase-2-locations-discovery.md)
- **Features:** `location-search.feature`, `location-intelligence.feature`

### Phase 3 — Interest tracking
- Mark/unmark interest; interest counts; sort by consensus.
- **Features:** `interest-tracking.feature`

### Phase 4 — Off-cycles & attendance ✅ built
- Create/manage off-cycle events (location + dates); `EndDate >= StartDate` enforced; creator seeded `Interested`.
- Per-user attendance status (five values, unrestricted transitions); per-off-cycle roster + counts.
- Heuristic, date-specific cost estimates (distance-based flights, per-night accommodation/expenses),
  recomputed per request and reflecting the traveller's home location; confidence + timestamp disclosed.
- React off-cycles screen wired via RTK Query (plan, list, set attendance).
- Reqnroll suite green (36/36). **Details:** [docs/11-phase-4-offcycles-attendance.md](docs/11-phase-4-offcycles-attendance.md)
- **Features:** `off-cycle-planning.feature`, `attendance-status.feature`, `cost-estimation.feature`

### Phase 5 — Hardening
- Telemetry dashboards, error taxonomy, accessibility pass, load smoke.
- Cross-cutting principle scenarios (equal access, transparency, permanence) all green.

## Out of scope (Future Enhancements)

Slack integration, real-time flight pricing/tracking, dedicated visa API, guest accounts,
budget pools, time-zone overlap scoring, and the MAUI mobile app are **not** in this plan.
They are captured so the schema and API leave room for them (see domain model notes).

## Definition of done (per feature)

- [ ] Gherkin feature file reviewed and committed.
- [ ] Step definitions drive the real system (no mocked domain).
- [ ] All scenarios green locally and in CI.
- [ ] Unit tests cover domain invariants / state transitions.
- [ ] EF migration created and applied by the migration service.
- [ ] React screen wired via RTK Query with loading/error states.
- [ ] Telemetry (traces/logs) visible in the Aspire dashboard.
- [ ] Docs updated if contracts changed.
