# 09 — Phase 1: Authentication & profiles (built)

Phase 1 is implemented and green. It adds persistence, Google sign-in with the domain
restriction, and full user-profile management. The BDD suite now runs **11 scenarios** across
three feature files.

## What was added

| Project | Path | Role |
| --- | --- | --- |
| Domain | [src/CycleSync.Domain](../src/CycleSync.Domain) | `User` aggregate, `Passport`, `GeoPlace` value object |
| Infrastructure | [src/CycleSync.Infrastructure](../src/CycleSync.Infrastructure) | `CycleSyncDbContext`, EF config, `InitialCreate` migration |
| MigrationService | [src/CycleSync.MigrationService](../src/CycleSync.MigrationService) | Applies migrations on startup, then completes |
| API (auth) | [src/CycleSync.Api/Auth](../src/CycleSync.Api/Auth) | Google token validation, domain policy, JWT session, current-user |
| API (features) | [src/CycleSync.Api/Features](../src/CycleSync.Api/Features) | Auth, Profile, Users, Locations (stub) endpoints |

## Green scenarios (11 total)

- `smoke.feature` — 2 (unchanged from Phase 0)
- [`authentication.feature`](../tests/features/authentication.feature) — 4: allowed-domain sign-in,
  disallowed-domain rejection, unauthenticated `401`, and equal-access (no admins).
- [`user-profile.feature`](../tests/features/user-profile.feature) — 5: first-sign-in
  provisioning, home/currency/language, add/remove passports, cross-user visibility.

```
Passed!  - Failed: 0, Passed: 11, Skipped: 0, Total: 11 - CycleSync.Acceptance.dll (net10.0)
```

## How authentication works

1. The SPA obtains a Google ID token, then calls `POST /api/auth/google { idToken }`.
2. `IGoogleTokenValidator` validates the token (real implementation uses Google's keys).
3. `WorkspaceAccessPolicy` enforces the allowed email domain(s) — otherwise `403` with
   `detail: "domain not permitted"`.
4. First-time users are **provisioned** (profile seeded from the Google name/email).
5. `TokenService` issues a short-lived **application session JWT** (HS256). All later requests
   send it as a bearer token; `JwtBearer` validates it and rejects anonymous calls with `401`.

There are **no roles** — a single authenticated authorization policy protects every endpoint,
enforcing the "equal access" principle. The equal-access scenario asserts the issued tokens
carry no `role`/`admin` claim.

## Endpoints added

| Method | Path | Notes |
| --- | --- | --- |
| POST | `/api/auth/google` | Sign in (anonymous); enforces domain, provisions, returns JWT |
| GET | `/api/auth/me` | Current user's profile |
| GET/PUT | `/api/me/profile` | View / update home location, currency, language |
| GET/POST | `/api/me/passports` | List / add passports |
| DELETE | `/api/me/passports/{country}` | Remove a passport |
| GET | `/api/users`, `/api/users/{id}` | All profiles are visible (privacy-friendly) |
| GET | `/api/locations` | Protected stub (empty) — Phase 2 fills it in |

## Persistence

- **SQL Server** in production via `Microsoft.EntityFrameworkCore.SqlServer`; the connection
  string is injected by the Aspire AppHost. The `InitialCreate` migration creates `Users` and
  `Passports` (unique index on email, and on `(UserId, Country)`).
- The AppHost now runs `migrations` before the API (`WaitForCompletion`).

## Testing approach (no Docker required)

Because this environment has no container runtime, the acceptance suite runs the **real API**
against an **in-memory SQLite** database (schema created from the EF model) and a
**deterministic fake Google validator** — both swapped in by
[`CycleSyncApiFactory`](../tests/CycleSync.Acceptance/Support/CycleSyncApiFactory.cs) via
`ConfigureTestServices`. The domain-restriction, provisioning, JWT, and profile logic exercised
are the real production code paths; only the external SQL Server and Google are substituted.

> Per [06-bdd-strategy.md](06-bdd-strategy.md), the intended CI setup runs these same scenarios
> against a **Testcontainers SQL Server** for full-fidelity integration. The SQLite swap is the
> offline-friendly equivalent used here.

## Next: Phase 2

Location search (Azure Maps) and AI location intelligence — link `location-search.feature` and
`location-intelligence.feature`, and add the `Location` aggregate with permanence enforced.
