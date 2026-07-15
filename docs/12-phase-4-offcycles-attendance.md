# 11 — Phase 4: Off-cycles & attendance (built)

Phase 4 is implemented and green. It adds the **OffCycle** aggregate (a concrete meetup: a
permanent location plus a date range), a per-user **Attendance** state, and a heuristic
**cost estimate** that is recalculated for specific dates and reflects each traveller's home
location. The BDD suite now runs **36 scenarios** across eight feature files.

> Phase 3 (interest tracking) is not part of this slice; `interest-tracking.feature` remains
> unwired. Off-cycles and attendance do not depend on interest, so Phase 4 was built directly.

## What was added

| Project | Path | Role |
| --- | --- | --- |
| Domain | [src/CycleSync.Domain/OffCycles](../src/CycleSync.Domain/OffCycles) | `OffCycle` aggregate, `Attendance`, `AttendanceStatus` (+ display labels) |
| Domain | [DomainValidationException](../src/CycleSync.Domain/DomainValidationException.cs) | Invariant breach → surfaced as `validation` problem detail |
| Infrastructure | [src/CycleSync.Infrastructure](../src/CycleSync.Infrastructure) | EF config for `OffCycle`/`Attendance`, `AddOffCycles` migration |
| API (features) | [src/CycleSync.Api/Features/OffCycles](../src/CycleSync.Api/Features/OffCycles) | Off-cycle CRUD, attendance PUT/GET, date-specific cost estimate + contracts |
| API (features) | [src/CycleSync.Api/Features/Cost](../src/CycleSync.Api/Features/Cost) | Shared cost-estimate builder (also used by the location estimate) |
| API (integrations) | [src/CycleSync.Api/Integrations/Cost](../src/CycleSync.Api/Integrations/Cost) | `ICostEstimator` abstraction + heuristic implementation |
| Web (SPA) | [src/web/src/features/offcycles](../src/web/src/features/offcycles) | Off-cycles screen wired via RTK Query: plan, list, set attendance |

## Green scenarios (41 total)

- Phases 0–3 unchanged — 24 (adds Phase 3 interest tracking, merged from main).
- [`off-cycle-planning.feature`](../tests/features/off-cycle-planning.feature) — 4: create an
  off-cycle, creator seeded `Interested`, reject `end < start`, and edit dates (cost recalculated).
- [`attendance-status.feature`](../tests/features/attendance-status.feature) — 8: set each of the
  five statuses, free progression and withdrawal, reject unknown status, and per-off-cycle summary.
- [`cost-estimation.feature`](../tests/features/cost-estimation.feature) — 4: an estimate is
  produced, confidence + generation time disclosed, recalculated for the off-cycle's nights, and it
  differs by the traveller's home location.

```
Passed!  - Failed: 0, Passed: 41, Skipped: 0, Total: 41 - CycleSync.Acceptance.dll (net10.0)
```

## Endpoints added

| Method | Path | Notes |
| --- | --- | --- |
| POST | `/api/off-cycles` | Create `{ name, locationId, startDate, endDate }`; creator seeded `Interested`; `end < start` → `400 validation` |
| GET | `/api/off-cycles` | All off-cycles (privacy-friendly; visible to everyone) |
| GET | `/api/off-cycles/{id}` | Off-cycle detail |
| PUT | `/api/off-cycles/{id}` | Edit name/dates; re-validates the range (estimates recompute on read) |
| PUT | `/api/off-cycles/{id}/attendance` | Set my status `{ status }` (idempotent); unknown → `400 validation` |
| GET | `/api/off-cycles/{id}/attendance` | Roster + per-status counts (keyed by display label) |
| GET | `/api/off-cycles/{id}/cost-estimate` | Date-specific estimate for the current user |
| GET | `/api/locations/{id}/cost-estimate` | Generic (nominal-stay) estimate for the current user |

## Attendance status

The five statuses are `Interested`, `Can't Make It`, `Probably Coming`, `Definitely Coming`, and
`Booked` (stored as a `tinyint`; display labels on the wire). Per the domain model, **transitions
are unrestricted** between the known values — a person's plans change freely, so the only rules are:

- The value must be one of the five (unknown → rejected with `unknown attendance status`).
- Creating an off-cycle seeds the creator as `Interested`.

`GET .../attendance` returns a roster and a `counts` map keyed by display label, e.g.
`{ "Booked": 1, "Definitely Coming": 1, "Can't Make It": 1 }`.

## How cost estimation works

`ICostEstimator` turns *who is asking + where + for how long* into an estimate. The default
`HeuristicCostEstimator` is a **deterministic, offline** stand-in so the feature runs without a paid
flight provider:

- **Flights** scale with the great-circle (haversine) distance from the traveller's **home** to the
  destination — so two travellers with different home locations get different flight figures. The
  home is geocoded through `IMapsSearch`; if it cannot be resolved, a stable name-derived fallback
  keeps the figure deterministic and still home-specific.
- **Accommodation** and **daily expenses** scale with the number of **nights** (`EndDate − StartDate`
  for an off-cycle; a nominal 3-night stay for a location's generic estimate).
- Amounts are expressed in the user's `PreferredCurrency` via a small multiplier table, and the
  estimate reports `Medium` confidence and a `generatedAt` timestamp (transparency principle).

Estimates are **recomputed per request** rather than cached, so they always reflect the current
dates and the caller's home/currency — editing an off-cycle's dates needs no cache invalidation. An
`ICostEstimator` backed by a real flight-price provider plugs in behind the same interface.

## Persistence

The `AddOffCycles` migration adds two tables:

- **OffCycles** — `StartDate`/`EndDate` as `date`, a check constraint `EndDate >= StartDate`, and a
  restrict-on-delete FK to the permanent location.
- **Attendances** — composite PK `(OffCycleId, UserId)` (one status per user per off-cycle),
  `Status` as `tinyint`, cascade-on-delete FK to the off-cycle.

`Attendance` is mapped as a normal child entity (not an EF *owned* type): EF reliably tracks
additions to a normal collection as inserts, whereas a newly added row in an owned collection can be
mis-detected as an update. The attendance write endpoint loads the roster with `Include` so a first
status is inserted and an existing one updated.

## Frontend (SPA)

The React app adds an **Off-cycles** screen ([OffCyclesPage.tsx](../src/web/src/features/offcycles/OffCyclesPage.tsx))
wired via RTK Query, reachable from a new top nav:

- **Plan an off-cycle** — name, a location picker (from saved locations), and start/end dates;
  invalid dates surface an inline error from the API's `validation` problem detail.
- **Planned off-cycles** — each shows its location, dates, nights, and a live per-status attendance
  summary, plus a dropdown to set my own attendance (idempotent `PUT`).

Component tests run under **Vitest + Testing Library** in jsdom: a fetch stand-in drives RTK Query
so the list, create (`POST`), and set-attendance (`PUT`) flows are covered headlessly. Run them with
`npm test` in `src/web`.

## Testing approach

The acceptance suite runs the **real API** against **in-memory SQLite** with the same offline
doubles as Phase 2. Two touches support Phase 4:

- `FakeMapsSearch` gained a **London** entry so a second traveller's home geocodes to real
  coordinates, making the "flights differ by home location" scenario geographically meaningful.
- A single shared *"it is visible to all users"* step now dispatches on the last-created entity
  (location or off-cycle), and a shared *"the operation is rejected with reason"* step asserts the
  `validation` problem detail — both bound once to avoid ambiguous step matches.

## Next

Phase 5 — hardening: telemetry dashboards, error taxonomy, accessibility, load smoke, and the
cross-cutting principle scenarios. Phase 3 (interest tracking) also remains outstanding.
