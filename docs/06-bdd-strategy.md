# 06 — BDD strategy

CycleSync is built **BDD-first**. The Gherkin feature files in
[../tests/features/](../tests/features/) are the specification, the acceptance tests, and the
living documentation. No feature is "done" until its scenarios are green in CI.

## Tooling

| Layer | Tool | Role |
| --- | --- | --- |
| Gherkin runner (backend) | **Reqnroll** (SpecFlow successor) + xUnit | Executes `.feature` files as acceptance tests against the real API |
| API-level driving | `WebApplicationFactory` / Aspire test host + `HttpClient` | Steps call real endpoints |
| Real database | **Testcontainers** SQL Server | Integration/BDD run against real SQL Server, migrations applied |
| UI-level driving | **Playwright for .NET** | `@ui` scenarios drive the React app in a browser |
| Unit tests | xUnit | Domain invariants, state machine, cost heuristic |
| Component tests | Vitest + RTL | React components in isolation |

## Test pyramid

```
        ▲  few   E2E / @ui BDD (Playwright) — critical journeys, principle checks
       ╱ ╲
      ╱   ╲     API BDD (Reqnroll → HttpClient → real API → Testcontainers SQL)
     ╱     ╲    ← the bulk of acceptance coverage lives here
    ╱───────╲
   ╱  many   ╲  Unit tests (domain, state machine, heuristics) + React component tests
  ╱───────────╲
```

Most scenarios run at the **API level** (fast, deterministic, real DB). Only scenarios tagged
`@ui` (e.g. `smoke.feature` shell load) additionally drive the browser via Playwright, so the
same Gherkin can be bound to either an API or UI step implementation as needed.

## Determinism: external integrations

Azure Maps and the LLM are **stubbed in test configuration** so scenarios are offline and
repeatable:

- `Given Azure Maps returns results for "Lisbon"` configures a fake `IMapsSearch`.
- `LocationIntelligence` / `CostEstimator` use deterministic test doubles that still populate
  `confidence` and `generatedAt`, so transparency scenarios remain meaningful.
- `TimeProvider` is a controllable fake so "generated 1 day ago" / stale-cache scenarios are
  exact.

Google OIDC is exercised against a stub identity provider (or a signed test token) so the
domain-restriction scenarios run without contacting Google.

## Step organisation

- One step-definition class per feature area, sharing a `ScenarioContext`.
- A `SystemFixture` (collection fixture) boots the Aspire test host + Testcontainers SQL once
  per run; each scenario gets a clean data state (respawn/truncate between scenarios, except
  `Locations` which is treated as append-only to honour permanence).
- Common steps (`Given a signed-in user "…"`, `Then the response status is <n>`) live in a
  shared steps assembly.

## Mapping features → phases

| Feature file | Phase |
| --- | --- |
| `smoke.feature` | 0 |
| `authentication.feature`, `user-profile.feature` | 1 |
| `location-search.feature`, `location-intelligence.feature` | 2 |
| `interest-tracking.feature` | 3 |
| `off-cycle-planning.feature`, `attendance-status.feature`, `cost-estimation.feature` | 4 |

Principle scenarios (`@principle-equal-access`, `@principle-transparency`,
`@principle-permanence`, `@privacy`) are distributed across features and all must be green by
end of Phase 5.

## The red→green loop (per scenario)

1. Write/extend the `.feature` scenario; review as documentation.
2. Implement step definitions that call the real system — scenario is **red**.
3. Build the vertical slice until **green**.
4. Refactor with the suite green.
5. Keep the scenario in CI as a permanent regression gate.

## CI gates

On every PR:
1. `dotnet build` + unit tests.
2. Reqnroll acceptance suite (spins up Testcontainers SQL + Aspire test host).
3. `@ui` Playwright scenarios (browser binaries cached — see `playwright-ci-caching`).
4. Frontend `vitest` + `tsc --noEmit` + lint.
5. Merge blocked unless all feature scenarios pass. A living-doc report (Reqnroll output) is
   published as a build artifact.

## Tag reference

| Tag | Meaning |
| --- | --- |
| `@phase0..4` | Delivery phase |
| `@ui` | Requires Playwright / browser |
| `@auth`, `@profile`, `@locations`, `@interest`, `@offcycle`, `@attendance`, `@cost` | Feature area |
| `@principle-*`, `@privacy` | Design-principle acceptance |
