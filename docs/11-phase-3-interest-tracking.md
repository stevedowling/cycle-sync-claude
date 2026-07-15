# 11 — Phase 3: Interest tracking (built)

Phase 3 is implemented and green. It adds the **Interest** join between users and locations, so the
team can converge on consensus destinations. Every location now exposes a team-wide **interest
count** and whether the current user is interested, and the location list can be returned in
**consensus order**. The BDD suite now runs **24 scenarios** across six feature files.

## What was added

| Project | Path | Role |
| --- | --- | --- |
| Domain | [src/CycleSync.Domain/Interests](../src/CycleSync.Domain/Interests) | `Interest` entity (idempotent mark, composite identity) |
| Infrastructure | [src/CycleSync.Infrastructure](../src/CycleSync.Infrastructure) | `InterestConfiguration`, `AddInterests` migration |
| API (features) | [src/CycleSync.Api/Features/Interests](../src/CycleSync.Api/Features/Interests) | Mark/remove interest + `me/interests` endpoints |
| API (features) | [src/CycleSync.Api/Features/Locations](../src/CycleSync.Api/Features/Locations) | List/detail enriched with `interestCount` + `isInterested`; `?sort=interest` |
| Web (SPA) | [src/web/src/features/locations](../src/web/src/features/locations) | Per-destination interest toggle and consensus sort, wired via RTK Query |
| E2E | [tests/e2e](../tests/e2e) | Playwright interest + consensus-sort spec (runs in CI) |

## Green scenarios (24 total)

- `smoke.feature` — 2, `authentication.feature` — 4, `user-profile.feature` — 5,
  `location-search.feature` — 4, `location-intelligence.feature` — 4 (unchanged).
- [`interest-tracking.feature`](../tests/features/interest-tracking.feature) — 5: mark interest,
  marking is idempotent, remove interest, counts aggregate across the team, and locations sort by
  consensus.

```
Passed!  - Failed: 0, Passed: 24, Skipped: 0, Total: 24 - CycleSync.Acceptance.dll (net10.0)
```

## Endpoints added

| Method | Path | Notes |
| --- | --- | --- |
| PUT | `/api/locations/{id}/interest` | Mark interest — idempotent → `204`; `404` if the location is unknown |
| DELETE | `/api/locations/{id}/interest` | Remove interest — idempotent → `204` |
| GET | `/api/me/interests` | The caller's interested locations, in consensus order |

`GET /api/locations` and `GET /api/locations/{id}` now include **`interestCount`** (team-wide) and
**`isInterested`** (for the caller). `GET /api/locations?sort=interest` returns the persisted
locations in descending interest order, with name as a stable tie-break.

## How interest works

1. Marking interest `PUT`s to `/api/locations/{id}/interest`. It is **idempotent**: a second mark by
   the same user is a no-op that still returns `204`. The composite primary key `(UserId,
   LocationId)` enforces one row per user per location at the database level.
2. Removing interest `DELETE`s the row (also idempotent — removing when not interested returns
   `204`).
3. `interestCount` for a location is simply the number of interest rows, computed with a
   `GROUP BY LocationId` aggregation. No user's interest weighs more than another's — the **Equal
   Access** principle.
4. Consensus sort orders by that count descending. Because interest is read straight from the same
   table, counts and the sort are always consistent.

## Persistence

- One table added by the `AddInterests` migration: **Interests** (`UserId`, `LocationId`,
  `CreatedAt`), with a **composite PK** `(UserId, LocationId)` for idempotency and a non-unique index
  on `LocationId` for the count / consensus-sort queries.
- Both foreign keys (to `Users` and `Locations`) are **restrict-on-delete** — consistent with
  location permanence and avoiding multiple cascade paths on SQL Server.
- The migration service applies it on startup, as in earlier phases.

## Testing approach

The acceptance suite continues to run the **real API** against **in-memory SQLite**. The new
[`InterestSteps`](../tests/CycleSync.Acceptance/Steps/InterestSteps.cs) drive the real endpoints, and
the multi-user scenarios reuse [`ScenarioWorld`](../tests/CycleSync.Acceptance/Support/ScenarioWorld.cs)'s
session switching to have other teammates (`bao`, `carlos`) sign in and mark interest before the
counts and consensus order are asserted as the original user. `Tallinn, Estonia` was added to the
offline gazetteers ([`FakeMapsSearch`](../tests/CycleSync.Acceptance/Support/FakeMapsSearch.cs) and
[`OfflineMapsSearch`](../src/CycleSync.Api/Integrations/Maps/OfflineMapsSearch.cs)) so a second
destination is available for the ranking scenarios.

## Frontend (SPA)

[`LocationsPage`](../src/web/src/features/locations/LocationsPage.tsx) now shows, for each saved
destination, an **interest count** and a **toggle** (★ Interested / ☆ Mark interest) that marks or
removes interest via RTK Query mutations, invalidating the `Locations` cache so counts refresh. A
**Sort by interest** checkbox re-queries `/api/locations?sort=interest` to display consensus order.
The RTK Query mutations (`markInterest`, `removeInterest`) live in
[apiSlice.ts](../src/web/src/features/api/apiSlice.ts). Vitest + Testing Library cover the count
display, the mark-interest `PUT`, and the consensus re-query headlessly.

## End-to-end & CI

The CI jobs are unchanged in shape — the acceptance job automatically picks up the new feature file,
and the [Playwright spec](../tests/e2e/specs/locations.e2e.spec.ts) gains a second test that signs
in, persists two destinations, marks interest, and asserts the higher-interest destination ranks
first under consensus sort. It runs in the same hermetic `E2E` environment (offline auth + offline
maps, SPA served same-origin) as Phase 2.

## Next: Phase 4

Off-cycles & attendance — create off-cycle events (location + dates), a per-user attendance status
state machine, and date-specific cost estimates (`off-cycle-planning.feature`,
`attendance-status.feature`, `cost-estimation.feature`).
