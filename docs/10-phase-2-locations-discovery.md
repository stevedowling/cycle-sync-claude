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

## Deferred (consistent with Phase 1)

The React locations/intelligence screen and Playwright UI coverage remain deferred; the Phase 2 BDD
suite is API-level. Wiring the SPA screens via RTK Query and adding Playwright scenarios is tracked
for a later hardening pass (Phase 5).

## Next: Phase 3

Interest tracking — mark/unmark interest, interest counts, and consensus sort over the persisted
locations (`interest-tracking.feature`).
