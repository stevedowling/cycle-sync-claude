# 05 — Frontend SPA (React)

A single-page application in **React 18 + TypeScript**, built with **Vite**, data-fetched via
**RTK Query**, routed with **React Router**. The SPA holds no third-party secrets — all maps
and LLM calls go through the API.

## Stack

| Concern | Choice |
| --- | --- |
| Build/dev | Vite |
| Language | TypeScript (strict) |
| Data/cache | Redux Toolkit + RTK Query (auto-generated hooks per endpoint) |
| Routing | React Router (data router) |
| Forms | React Hook Form + Zod validation |
| UI | Component library of choice (e.g. Mantine/MUI) + design tokens |
| Auth | Redirect to `/api/auth/login`; session cookie; `useMe()` gate |
| Testing | Vitest + React Testing Library (component); Playwright (E2E, drives BDD UI steps) |

## App shell & routing

```
/                       → Dashboard (locations by consensus, my off-cycles)
/locations              → Location list + search
/locations/:id          → Location detail (intelligence, cost, interest toggle)
/off-cycles             → Off-cycle list
/off-cycles/new         → Create off-cycle
/off-cycles/:id         → Off-cycle detail (attendance, date-specific costs)
/profile                → My profile (home, currency, language, passports)
/users/:id              → View another user's profile
/login                  → Unauthenticated landing → Google sign-in
```

A route guard calls `GET /api/auth/me`; unauthenticated users are redirected to `/login`.
Because access is flat, there are no role-gated routes.

## Screens ↔ features

| Screen | Backing features | Key API calls |
| --- | --- | --- |
| Login | `authentication.feature` | `/api/auth/login`, `/api/auth/me` |
| Profile | `user-profile.feature` | `/api/me/profile`, `/api/me/passports` |
| Location list/search | `location-search.feature`, `interest-tracking.feature` | `/api/locations`, `/search`, `/interest` |
| Location detail | `location-intelligence.feature`, `cost-estimation.feature` | `/intelligence`, `/cost-estimate` |
| Off-cycle create/edit | `off-cycle-planning.feature` | `POST/PUT /api/off-cycles` |
| Off-cycle detail | `attendance-status.feature`, `cost-estimation.feature` | `/attendance`, `/cost-estimate` |

## State & data patterns

- **Server state** lives in RTK Query cache; components subscribe via generated hooks
  (`useGetLocationsQuery`, `useSetAttendanceMutation`, …). Tags drive cache invalidation
  (e.g. setting attendance invalidates the off-cycle detail + attendance summary).
- **UI state** (modals, sort selection) stays local or in lightweight slices.
- Every query surfaces `isLoading` / `isError`; screens render skeletons and error banners.

## Transparency in the UI (principle)

Any AI-generated figure — intelligence blurbs and every cost line — renders with a
**confidence badge** and a **"generated <relative time> ago"** caption, sourced directly from
the API's `confidence` / `generatedAt` fields. This is a visible acceptance criterion in
`location-intelligence.feature` and `cost-estimation.feature`.

## Attendance control

A segmented control offering the five statuses (Interested, Can't Make It, Probably Coming,
Definitely Coming, Booked). Selecting one fires `PUT …/attendance`; the roster and per-status
counts update via cache invalidation. Any transition is allowed (see domain model).

## Accessibility & i18n

- WCAG AA: keyboard-navigable controls, labelled inputs, sufficient contrast.
- Currency/number/date formatting respects the user's `preferredLanguage` and
  `preferredCurrency`.

## Build & serve

- **Dev**: Vite dev server launched as an Aspire resource; API base URL injected via env.
- **Prod**: `vite build` → static assets served behind the reverse proxy; same-origin `/api`.
