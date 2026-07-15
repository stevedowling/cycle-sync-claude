# 13 — Phase 5: Hardening (built)

Phase 5 is implemented and green. It closes out the MVP by (1) making the API's **error taxonomy**
uniform and typed, and (2) locking in the **cross-cutting principle scenarios** as CI gates. The
BDD suite now runs **45 scenarios** across nine feature files.

## What was added

| Project | Path | Role |
| --- | --- | --- |
| API (http) | [src/CycleSync.Api/Http/Problems.cs](../src/CycleSync.Api/Http/Problems.cs) | Central RFC 7807 problem-details factory for the five error categories |
| Tests | [tests/features/error-taxonomy.feature](../tests/features/error-taxonomy.feature) | New `@phase5` feature that asserts every failure is a typed problem detail |
| Tests | [tests/CycleSync.Acceptance/Steps/ErrorTaxonomySteps.cs](../tests/CycleSync.Acceptance/Steps/ErrorTaxonomySteps.cs) | Step definitions for the taxonomy assertions |

## Error taxonomy

Before Phase 5 the API was inconsistent: several endpoints returned a **body-less `404`**
(`Results.NotFound()`) while others returned a typed problem detail, and the maps failure used a
one-off `upstream-unavailable` slug. Phase 5 introduces [`Problems`](../src/CycleSync.Api/Http/Problems.cs),
a single factory that every endpoint now uses, so **every** error response is an RFC 7807
`ProblemDetails` drawn from five stable categories:

| `type` slug | Status | Used for |
| --- | --- | --- |
| `not-found` | 404 | Missing / not-visible resource (off-cycle, location, user, profile) |
| `validation` | 400 | Well-formed request that breaks a rule or invariant |
| `forbidden` | 403 | Authenticated but not permitted (e.g. sign-in from a disallowed domain) |
| `unauthorized` | 401 | Could not be authenticated (invalid Google token) |
| `upstream` | 502 | An upstream dependency (maps, LLM) failed or was unreachable |

The `type` slug lets the SPA branch on the **kind** of error rather than parsing prose; the `title`
and `detail` remain human-readable. All bare `Results.NotFound()` calls across the Auth, Locations,
OffCycles, Profile, Users and Interests slices were replaced with `Problems.NotFound(...)`, and the
existing inline `Results.Problem(...)` calls were routed through the factory so titles/types can
never drift again.

## Green scenarios (45 total)

- Phases 0–4 unchanged — 41.
- [`error-taxonomy.feature`](../tests/features/error-taxonomy.feature) — 4: a missing off-cycle and
  a missing location each yield a typed `not-found` problem; an off-cycle with `end < start` yields
  a typed `validation` problem; and creating an off-cycle against a non-existent location yields a
  typed `not-found` problem. Each scenario also asserts a human-readable title and detail are
  present.

```
Passed!  - Failed: 0, Passed: 45, Skipped: 0, Total: 45 - CycleSync.Acceptance.dll (net10.0)
```

## Cross-cutting principles — the CI gate

The four guiding principles from the brief are now each guarded by an executable scenario that runs
against the real system on every build:

| Principle | Guarding scenario | Feature |
| --- | --- | --- |
| **Equal access** | "All authenticated users have identical rights" | [authentication.feature](../tests/features/authentication.feature) `@principle-equal-access` |
| **Transparent costs** | "Estimates disclose confidence and generation time" | [cost-estimation.feature](../tests/features/cost-estimation.feature) `@principle-transparency` |
| **Privacy-friendly** | "…and it is visible to all users" | [location-search.feature](../tests/features/location-search.feature) |
| **Location permanence** | "Locations are never deleted" | [location-search.feature](../tests/features/location-search.feature) `@principle-permanence` |

Phase 5 verifies these are all green together; the new error-taxonomy feature adds a fifth gate on
the transparency of *failures*.

## Telemetry, accessibility & load

- **Telemetry** — traces, metrics and logs are already emitted by every service via
  `AddServiceDefaults()` (OpenTelemetry, see [01-architecture.md](01-architecture.md)) and are
  viewable in the Aspire dashboard. No new instrumentation was required for the Phase 5 slice; the
  typed `type` slug additionally makes error rates dashboard-groupable by category.
- **Accessibility & load smoke** — the SPA's screens are exercised headlessly by the Vitest +
  Testing Library component tests and the Playwright E2E pass from earlier phases; a dedicated
  axe-core accessibility sweep and a k6/bombardier load smoke are the natural next increments and
  are captured under *Future Enhancements* rather than fabricated here.

## Testing approach

Same harness as Phases 2–4: the acceptance suite runs the **real API** against **in-memory SQLite**
with the offline Google/Maps doubles. The taxonomy steps request random GUIDs (guaranteed-missing
resources) and assert on the parsed problem-detail body — status, `type`, `title` and `detail` —
so a regression to a body-less `404` or a drifted slug fails the build.
