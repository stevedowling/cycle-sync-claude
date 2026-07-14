# 04 — API design

REST over HTTPS, JSON, ASP.NET Core Minimal APIs. All endpoints (except auth callbacks and
health) require an authenticated session under a single flat policy (no roles).

## Conventions

- Base path `/api`. Resource-oriented URLs, plural nouns.
- `camelCase` JSON. Dates as ISO-8601; `DateOnly` fields as `yyyy-MM-dd`.
- Errors use RFC 7807 `application/problem+json` with a stable `type` taxonomy:
  `validation`, `not-found`, `forbidden`, `conflict`, `upstream-unavailable`.
- Idempotent writes where the domain allows (marking interest, setting attendance).
- Every response carrying an estimate includes `confidence` and `generatedAt`.

## Auth

| Method | Path | Purpose |
| --- | --- | --- |
| GET | `/api/auth/login` | Begin Google OIDC (authorization code + PKCE) |
| GET | `/api/auth/callback` | OIDC callback; validate token, enforce domain, issue session |
| POST | `/api/auth/logout` | End session |
| GET | `/api/auth/me` | Current user's identity + profile summary |

Domain restriction (`hd`/email allow-list) enforced here. Non-allowed domain → `403` with
`type: forbidden`, reason "domain not permitted".

## Users & profile

| Method | Path | Purpose |
| --- | --- | --- |
| GET | `/api/users` | List users (privacy-friendly; all visible) |
| GET | `/api/users/{id}` | View a user's profile |
| GET | `/api/me/profile` | Current profile |
| PUT | `/api/me/profile` | Update home location, currency, language |
| GET | `/api/me/passports` | List my passports |
| POST | `/api/me/passports` | Add a passport `{ countryCode }` |
| DELETE | `/api/me/passports/{countryCode}` | Remove a passport |

## Locations

| Method | Path | Purpose |
| --- | --- | --- |
| GET | `/api/locations/search?q=` | Azure Maps search (proxied server-side) |
| POST | `/api/locations` | Persist a selected search result (de-duplicated) |
| GET | `/api/locations` | List persisted locations; `?sort=interest` for consensus order |
| GET | `/api/locations/{id}` | Location detail (core fields) |
| GET | `/api/locations/{id}/intelligence` | AI intelligence (cached; regenerated when stale) |
| GET | `/api/locations/{id}/cost-estimate` | Generic heuristic estimate for current user |

No `DELETE /api/locations/{id}` — Location Permanence. `POST /api/locations` returns the
existing location if it already exists (`200`) rather than duplicating (`201`).

## Interest

| Method | Path | Purpose |
| --- | --- | --- |
| PUT | `/api/locations/{id}/interest` | Mark interest (idempotent) → `204` |
| DELETE | `/api/locations/{id}/interest` | Remove interest → `204` |
| GET | `/api/me/interests` | My interested locations |

Location list/detail responses include `interestCount` and `isInterested`.

## Off-cycles

| Method | Path | Purpose |
| --- | --- | --- |
| POST | `/api/off-cycles` | Create `{ name, locationId, startDate, endDate }` |
| GET | `/api/off-cycles` | List all off-cycles |
| GET | `/api/off-cycles/{id}` | Detail incl. attendance summary + my status |
| PUT | `/api/off-cycles/{id}` | Edit name/dates (triggers cost recalculation) |
| GET | `/api/off-cycles/{id}/cost-estimate` | Date-specific estimate for current user |

Validation: `endDate >= startDate` → else `400` `type: validation`, reason
"end date must not precede start date". Creator seeded as `Interested`.

## Attendance

| Method | Path | Purpose |
| --- | --- | --- |
| PUT | `/api/off-cycles/{id}/attendance` | Set my status `{ status }` (idempotent) |
| GET | `/api/off-cycles/{id}/attendance` | Full roster + per-status counts |

Unknown status value → `400` `type: validation`, reason "unknown attendance status".

## Representative payloads

`GET /api/locations/{id}` :
```json
{
  "id": "…",
  "name": "Lisbon, Portugal",
  "country": "Portugal",
  "coordinates": { "latitude": 38.72, "longitude": -9.14 },
  "interestCount": 2,
  "isInterested": true
}
```

`GET /api/locations/{id}/cost-estimate` :
```json
{
  "currency": "NZD",
  "flights": 2450.00,
  "accommodation": 780.00,
  "dailyExpenses": 520.00,
  "nights": 4,
  "confidence": "Medium",
  "generatedAt": "2026-07-14T02:15:00Z"
}
```

`GET /api/off-cycles/{id}/attendance` :
```json
{
  "offCycleId": "…",
  "counts": { "Booked": 1, "DefinitelyComing": 1, "CantMakeIt": 1 },
  "roster": [
    { "userId": "…", "displayName": "Amara", "status": "Booked" }
  ]
}
```

## Error example

```json
{
  "type": "validation",
  "title": "Invalid off-cycle dates",
  "status": 400,
  "detail": "end date must not precede start date"
}
```
