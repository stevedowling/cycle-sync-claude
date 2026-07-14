# 02 — Domain model

The domain is small and revolves around five aggregates: **User**, **Location**,
**Interest**, **OffCycle**, and **Attendance**.

## Entities & value objects

### User
| Field | Type | Notes |
| --- | --- | --- |
| `Id` | Guid | Primary key |
| `Email` | string | Unique; from Google; drives domain restriction |
| `DisplayName` | string | From Google profile |
| `HomeLocation` | `GeoPlace` (value object) | Name + country + lat/long |
| `PreferredCurrency` | string (ISO 4217) | e.g. `NZD` |
| `PreferredLanguage` | string (BCP-47) | e.g. `en-NZ` |
| `Passports` | list of `Passport` | Country codes; drives visa guidance |
| `CreatedAt` / `UpdatedAt` | DateTimeOffset | Audit |

`Passport` = `{ CountryCode }`. A user may hold several; the set is unique per user.

### Location
Permanent destinations. **Never deleted** (Location Permanence principle).

| Field | Type | Notes |
| --- | --- | --- |
| `Id` | Guid | Primary key |
| `Name` | string | Display name, e.g. "Lisbon, Portugal" |
| `Country` | string | ISO country |
| `Coordinates` | `GeoCoordinates` | lat/long |
| `AzureMapsId` | string? | External identifier for de-duplication |
| `CreatedAt` | DateTimeOffset | Audit |

Associated read-optimised data (regenerated, not authoritative):
- **`LocationIntelligence`** — climate summary, best times to visit, travel tips, visa notes.
  Carries `GeneratedAt` and `Confidence`. Cached; regenerated when stale.
- **`CostEstimate`** — flights / accommodation / daily expenses. Carries `GeneratedAt`,
  `Confidence`, `Currency`, and the inputs (origin, dates) it was computed for.

### Interest
Join between a `User` and a `Location`. Unique per (user, location) — marking interest is
idempotent. Removing interest deletes the row. `InterestCount(location)` = number of rows.

| Field | Type |
| --- | --- |
| `UserId` | Guid |
| `LocationId` | Guid |
| `CreatedAt` | DateTimeOffset |

### OffCycle
A concrete planned meetup.

| Field | Type | Notes |
| --- | --- | --- |
| `Id` | Guid | Primary key |
| `Name` | string | e.g. "Autumn Meetup" |
| `LocationId` | Guid | The destination |
| `StartDate` | DateOnly | |
| `EndDate` | DateOnly | Invariant: `EndDate >= StartDate` |
| `CreatedByUserId` | Guid | The creator |
| `CreatedAt` / `UpdatedAt` | DateTimeOffset | |

**Invariants**
- `EndDate` must not precede `StartDate` (rejected with a domain error).
- Nights = `EndDate - StartDate` (used by date-specific cost recalculation).
- Editing dates triggers cost-estimate recalculation.

### Attendance
Per-user status for an off-cycle. Join between `User` and `OffCycle`.

| Field | Type |
| --- | --- |
| `OffCycleId` | Guid |
| `UserId` | Guid |
| `Status` | `AttendanceStatus` enum |
| `UpdatedAt` | DateTimeOffset |

## AttendanceStatus state machine

Statuses (from the brief):

```
Interested ─┐
            ├─▶ Probably Coming ─▶ Definitely Coming ─▶ Booked
Can't Make It┘        ▲                    │                │
     ▲                └────────────────────┴────────────────┘
     └───────── any status can move to "Can't Make It" (withdrawal) ────────┘
```

Design decision: **transitions are unrestricted between the five known values** — users can
move forward, backward, or withdraw at any time (a person's real plans change freely). The
only rules enforced are:

- The value must be one of the five known statuses (unknown → rejected).
- On creating an off-cycle, the creator is seeded as `Interested`.

Enum values: `Interested`, `CantMakeIt`, `ProbablyComing`, `DefinitelyComing`, `Booked`.
Display labels map to "Interested", "Can't Make It", "Probably Coming", "Definitely Coming",
"Booked".

> If stakeholders later want a stricter progression, tighten the state machine and add
> scenarios — the BDD suite is where that decision is recorded.

## Principle → invariant mapping

| Principle | Where enforced |
| --- | --- |
| Equal access | Flat auth policy; no role field on `User` |
| Transparent costs | `Confidence` + `GeneratedAt` required on intelligence & estimates |
| Privacy-friendly | No per-record ACLs; all reads unrestricted to authenticated users |
| Location permanence | No delete endpoint/handler for `Location`; enforced in tests |

## Extension seams (future enhancements)

Left deliberately open so future work is additive: Slack channel link on `OffCycle`,
real flight-price provider behind `ICostEstimator`, dedicated visa provider behind
intelligence, guest (view-only) users as a `User` sub-type, budget pool on `OffCycle`.
