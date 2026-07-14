# 10 — Phase 2: Locations & discovery (built)

Phase 2 is implemented and green. It adds the **Location** aggregate, Azure Maps-backed search,
permanent location persistence, and cached, timestamped **AI location intelligence** (climate,
best times to visit, travel tips, and passport-aware visa notes). The BDD suite now runs
**19 scenarios** across five feature files.

## What was added

| Project | Path | Role |
| --- | --- | --- |
| Domain | [src/CycleSync.Domain/Locations](../src/CycleSync.Domain/Locations) | `Location` aggregate, `LocationIntelligence`, `GeoCoordinates`, `Confidence` |
| Infrastructure | [src/CycleSync.Infrastructure](../src/CycleSync.Infrastructure) | EF config for both entities, `AddLocations` migration |
| API (features) | [src/CycleSync.Api/Features/Locations](../src/CycleSync.Api/Features/Locations) | Search, persist, list, detail, intelligence endpoints + contracts |
| API (integrations) | [src/CycleSync.Api/Integrations](../src/CycleSync.Api/Integrations) | `IMapsSearch` (Azure Maps) and `ILocationIntelligenceGenerator` abstractions + implementations |
| Web (SPA) | [src/web/src](../src/web/src) | React screens wired via RTK Query: sign-in, search/persist, and the intelligence detail |
| E2E | [tests/e2e](../tests/e2e) | Playwright full-stack spec (runs in CI) |
| CI | [.github/workflows/ci.yml](../.github/workflows/ci.yml) | Acceptance, web unit, SQL-Server fidelity, and Playwright jobs |

## Green scenarios (19 total)

- `smoke.feature` — 2, `authentication.feature` — 4, `user-profile.feature` — 5 (unchanged)
- [`location-search.feature`](../tests/features/location-search.feature) — 4: search returns
  destinations, selecting persists, de-duplication, and permanence (no delete).
- [`location-intelligence.feature`](../tests/features/location-intelligence.feature) — 4:
  intelligence is produced/timestamped, cached intelligence is reused, visa guidance for the
  user's passport, and transparency (confidence + freshness always disclosed).

```
Passed!  - Failed: 0, Passed: 19, Skipped: 0, Total: 19 - CycleSync.Acceptance.dll (net10.0)
```

## Endpoints added

| Method | Path | Notes |
| --- | --- | --- |
| GET | `/api/locations/search?q=` | Azure Maps search, proxied server-side; `502 upstream-unavailable` if the provider fails |
| POST | `/api/locations` | Persist a chosen result; de-duplicated → `200` (existing) or `201` (created) |
| GET | `/api/locations` | All persisted locations (privacy-friendly; visible to everyone) |
| GET | `/api/locations/{id}` | Location detail |
| GET | `/api/locations/{id}/intelligence` | Cached AI intelligence; regenerated only when stale |

There is deliberately **no** `DELETE /api/locations/{id}` — the Location Permanence principle. The
permanence scenario asserts that a delete attempt is rejected and the location survives.

## How search & persistence work

1. The SPA calls `GET /api/locations/search?q=`; the API calls Azure Maps behind `IMapsSearch`
   (the subscription key stays on the server).
2. Selecting a result `POST`s it to `/api/locations`. Persistence is **de-duplicated**: if a
   location with the same `AzureMapsId` (or the same `name`+`country`) already exists, the existing
   row is returned (`200`) instead of creating a duplicate (`201`). Unique indexes on
   `AzureMapsId` (filtered) and `(Name, Country)` back this up at the database level.
3. Locations are permanent — the aggregate exposes no delete behaviour and no endpoint maps it.

## How location intelligence works

- `ILocationIntelligenceGenerator` produces climate/best-times/tips/visa content for a location,
  tailored to the requesting user's passports. Output is cached as one `LocationIntelligence` row
  per location, carrying `GeneratedAt` and `Confidence`.
- On request, a **non-stale** cached row (younger than 30 days) is returned as-is; otherwise it is
  regenerated and replaces the previous row. The "cached intelligence is reused" scenario proves
  this by generating one day ago and asserting the returned timestamp is the earlier one.
- Every response carries `confidence` and `generatedAt`, satisfying the transparency principle.

### Intelligence provider note

The default `HeuristicLocationIntelligenceGenerator` is a **deterministic, offline** stand-in: it
templates plausible summaries from the location and passports without calling an external model, so
the feature is runnable and testable without secrets. Because it is templated rather than
model-generated it reports `Low` confidence. An LLM-backed implementation plugs in behind the same
`ILocationIntelligenceGenerator` interface to raise fidelity — no endpoint or schema change needed.

## Persistence

- Two tables added by the `AddLocations` migration: **Locations** (owned `GeoCoordinates` as inline
  `Latitude`/`Longitude`, filtered-unique `AzureMapsId`, unique `(Name, Country)`) and
  **LocationIntelligence** (one current row per location; `Confidence` stored as `tinyint`;
  restrict-on-delete FK to the permanent location).
- The migration service applies it on startup, as in Phase 1.

## Testing approach

The acceptance suite continues to run the **real API** against **in-memory SQLite** with two new
deterministic doubles swapped in by
[`CycleSyncApiFactory`](../tests/CycleSync.Acceptance/Support/CycleSyncApiFactory.cs):

- [`FakeMapsSearch`](../tests/CycleSync.Acceptance/Support/FakeMapsSearch.cs) — an offline gazetteer
  standing in for Azure Maps.
- [`ControllableTimeProvider`](../tests/CycleSync.Acceptance/Support/ControllableTimeProvider.cs) —
  a settable clock so a scenario can generate intelligence "1 day ago" and then prove it is reused.

The real intelligence generator (heuristic) is exercised directly — only the external SQL Server,
Google, and Azure Maps are substituted.

## Frontend (SPA)

The React app now has real screens wired via **RTK Query** ([src/web/src/features/api/apiSlice.ts](../src/web/src/features/api/apiSlice.ts)):

- **Sign-in** — a dev email sign-in that posts to `/api/auth/google`; the session token is stored in
  the `auth` slice and attached as a bearer on every request. In production the Google OIDC button
  replaces the form; the token flows through the same slice.
- **Locations** ([LocationsPage.tsx](../src/web/src/features/locations/LocationsPage.tsx)) — search
  destinations, add a result (persist), and browse the saved list, each with loading states.
- **Intelligence detail** ([LocationDetailPage.tsx](../src/web/src/features/locations/LocationDetailPage.tsx))
  — climate, best times, tips, and visa guidance, with the **confidence badge and generation
  timestamp always shown** (transparency principle).

Component tests run under **Vitest + Testing Library** in jsdom (no browser, no Docker): a fetch
stand-in drives RTK Query so the search→persist flow, the intelligence panel, and the auth gate are
covered headlessly. Run them with `npm test` in `src/web`.

## End-to-end & CI

A [GitHub Actions workflow](../.github/workflows/ci.yml) runs four jobs; none needs
Docker-in-Docker — GitHub's runners provide Docker at the host level:

1. **dotnet-acceptance** — the Reqnroll BDD suite (SQLite, offline).
2. **web-unit** — SPA lint, Vitest, and production build.
3. **sql-integration** — applies the EF migrations against a **real SQL Server** service container,
   catching anything that only works on SQLite (filtered indexes, collation, `datetimeoffset`,
   `tinyint`).
4. **e2e** — a **Playwright** spec drives the full stack (React → API → SQL Server) in a browser.

The E2E stack is hermetic: the API runs in a dedicated `E2E` environment
([Program.cs](../src/CycleSync.Api/Program.cs)) that swaps in an offline Google validator and an
offline maps gazetteer and serves the built SPA same-origin, so Playwright can sign in, search,
persist, and read intelligence without Google or Azure keys. These doubles are **only** active in
the `E2E` environment — never in Development or Production.

To run the E2E stack locally: build the SPA into `src/CycleSync.Api/wwwroot`, start the API with
`ASPNETCORE_ENVIRONMENT=E2E`, a SQL Server connection string, and `Auth__Jwt__SigningKey` set, then
`npx playwright test` from `tests/e2e` with `E2E_BASE_URL` pointing at it.

## Next: Phase 3

Interest tracking — mark/unmark interest, interest counts, and consensus sort over the persisted
locations (`interest-tracking.feature`).
