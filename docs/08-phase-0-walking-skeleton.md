# 08 — Phase 0: Walking skeleton (built)

Phase 0 is implemented and green. It proves the full stack wires together and establishes the
BDD harness that every later phase builds on.

## What exists

| Project | Path | Role |
| --- | --- | --- |
| AppHost | [src/CycleSync.AppHost](../src/CycleSync.AppHost) | Aspire orchestration: SQL Server, API, React (Vite) app |
| ServiceDefaults | [src/CycleSync.ServiceDefaults](../src/CycleSync.ServiceDefaults) | OpenTelemetry, health checks, resilience, service discovery |
| API | [src/CycleSync.Api](../src/CycleSync.Api) | ASP.NET Core Minimal API: `/health`, `/alive`, `/api/ping` |
| Web | [src/web](../src/web) | React 19 + TypeScript + Vite SPA shell that calls `/api/ping` |
| Acceptance | [tests/CycleSync.Acceptance](../tests/CycleSync.Acceptance) | Reqnroll + xUnit BDD harness driving the real API |

Solution: [CycleSync.slnx](../CycleSync.slnx). Framework: **.NET 10** (`net10.0`), pinned via
[global.json](../global.json). Packages are centrally managed in
[Directory.Packages.props](../Directory.Packages.props).

## The green scenario

[tests/features/smoke.feature](../tests/features/smoke.feature) runs as executable tests:

```
Passed!  - Failed: 0, Passed: 2, Skipped: 0, Total: 2 - CycleSync.Acceptance.dll (net10.0)
```

- **The API reports healthy** — boots the real API in-process via
  `WebApplicationFactory<Program>` and asserts `GET /health` → `200 "Healthy"`.
- **The SPA shell loads** — asserts the built (or source) SPA shell contains the CycleSync
  application shell (`<title>CycleSync</title>` + root element). See "Known limitations".

## How to build and run

Prerequisites: .NET 10 SDK, Node 20.19+ / 22.12+, and a container runtime (Docker) to run the
full AppHost.

```bash
# Restore & build everything
dotnet build CycleSync.slnx

# Run the BDD acceptance suite (no container needed — API runs in-process)
dotnet test tests/CycleSync.Acceptance

# Build the React SPA
cd src/web && npm install && npm run build

# Run the whole system under Aspire (needs Docker for SQL Server)
dotnet run --project src/CycleSync.AppHost
```

The API alone (no SQL dependency yet in Phase 0) can be run directly:

```bash
dotnet run --project src/CycleSync.Api
# then: curl http://localhost:5180/health   -> Healthy
#       curl http://localhost:5180/api/ping -> {"service":"CycleSync","status":"ok"}
```

## Known limitations (Phase 0)

- **`dotnet run` on the AppHost needs Docker.** The AppHost declares a SQL Server container
  resource. The API and acceptance suite do **not** need it in Phase 0 (no EF Core yet), so
  BDD runs fully without a container.
- **The `@ui` smoke scenario is verified without a browser.** It checks the SPA shell asset
  rather than driving a real browser. Phase 1+ wires **Playwright** for true `@ui` scenarios
  (see [06-bdd-strategy.md](06-bdd-strategy.md)); the Gherkin does not change, only the step
  binding is upgraded.

## BDD wiring notes (for later phases)

- Canonical `.feature` files live in [tests/features/](../tests/features) and are linked into
  the acceptance project as `ReqnrollFeatureFile` items (with `Link`), so there is one source
  of truth for docs and tests. Generated code-behind is routed to `obj/`
  (`ReqnrollUseIntermediateOutputPathForCodeBehind=true`).
- Only `smoke.feature` is linked today. Each later phase links its feature file(s) and adds
  the matching step definitions under `tests/CycleSync.Acceptance/Steps`.
- Per-scenario state (the running API + HTTP client) is provided by
  [`ScenarioWorld`](../tests/CycleSync.Acceptance/Support/ScenarioWorld.cs) via Reqnroll
  constructor injection.

## Next: Phase 1

Authentication & profiles — link `authentication.feature` and `user-profile.feature`, add
EF Core + the `CycleSync.MigrationService`, and stand up Google OIDC with domain restriction.
