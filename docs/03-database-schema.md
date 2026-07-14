# 03 — Database schema (SQL Server)

Code-first with EF Core 10; a dedicated `CycleSync.MigrationService` applies migrations on
startup. This document is the intended physical model — the migrations are the source of truth.

## Tables

### Users
| Column | Type | Constraints |
| --- | --- | --- |
| `Id` | `uniqueidentifier` | PK |
| `Email` | `nvarchar(320)` | NOT NULL, **unique index** |
| `DisplayName` | `nvarchar(200)` | NOT NULL |
| `HomeLocationName` | `nvarchar(200)` | NULL |
| `HomeCountry` | `nvarchar(100)` | NULL |
| `HomeLatitude` | `float` | NULL |
| `HomeLongitude` | `float` | NULL |
| `PreferredCurrency` | `char(3)` | NULL |
| `PreferredLanguage` | `nvarchar(20)` | NULL |
| `CreatedAt` | `datetimeoffset` | NOT NULL |
| `UpdatedAt` | `datetimeoffset` | NOT NULL |

Home location stored as owned/inline columns (EF owned type `GeoPlace`).

### Passports
| Column | Type | Constraints |
| --- | --- | --- |
| `Id` | `uniqueidentifier` | PK |
| `UserId` | `uniqueidentifier` | FK → Users, NOT NULL, ON DELETE CASCADE |
| `CountryCode` | `char(2)` | NOT NULL |

Unique index on (`UserId`, `CountryCode`).

### Locations
| Column | Type | Constraints |
| --- | --- | --- |
| `Id` | `uniqueidentifier` | PK |
| `Name` | `nvarchar(200)` | NOT NULL |
| `Country` | `nvarchar(100)` | NOT NULL |
| `Latitude` | `float` | NOT NULL |
| `Longitude` | `float` | NOT NULL |
| `AzureMapsId` | `nvarchar(200)` | NULL, **unique filtered index** (de-dup) |
| `CreatedAt` | `datetimeoffset` | NOT NULL |

No delete path (Location Permanence). Unique index also on (`Name`, `Country`) to prevent
duplicates when `AzureMapsId` is unavailable.

### LocationIntelligence
Cached AI output; one current row per location (history optional).
| Column | Type | Constraints |
| --- | --- | --- |
| `Id` | `uniqueidentifier` | PK |
| `LocationId` | `uniqueidentifier` | FK → Locations, NOT NULL |
| `ClimateSummary` | `nvarchar(max)` | NULL |
| `BestTimesToVisit` | `nvarchar(max)` | NULL |
| `TravelTips` | `nvarchar(max)` | NULL |
| `VisaNotes` | `nvarchar(max)` | NULL |
| `Confidence` | `tinyint` | NOT NULL (enum: Low/Medium/High) |
| `GeneratedAt` | `datetimeoffset` | NOT NULL |

Index on (`LocationId`, `GeneratedAt` desc).

### CostEstimates
| Column | Type | Constraints |
| --- | --- | --- |
| `Id` | `uniqueidentifier` | PK |
| `LocationId` | `uniqueidentifier` | FK → Locations, NOT NULL |
| `OffCycleId` | `uniqueidentifier` | FK → OffCycles, NULL (null = generic estimate) |
| `OriginUserId` | `uniqueidentifier` | FK → Users, NULL (per-traveller flights) |
| `Currency` | `char(3)` | NOT NULL |
| `FlightsAmount` | `decimal(12,2)` | NULL |
| `AccommodationAmount` | `decimal(12,2)` | NULL |
| `DailyExpensesAmount` | `decimal(12,2)` | NULL |
| `Nights` | `int` | NULL |
| `Confidence` | `tinyint` | NOT NULL |
| `GeneratedAt` | `datetimeoffset` | NOT NULL |

Index on (`LocationId`, `OffCycleId`, `OriginUserId`).

### Interests
| Column | Type | Constraints |
| --- | --- | --- |
| `UserId` | `uniqueidentifier` | FK → Users, NOT NULL |
| `LocationId` | `uniqueidentifier` | FK → Locations, NOT NULL |
| `CreatedAt` | `datetimeoffset` | NOT NULL |

**Composite PK** (`UserId`, `LocationId`) — enforces idempotency. Index on (`LocationId`) for
count/sort queries.

### OffCycles
| Column | Type | Constraints |
| --- | --- | --- |
| `Id` | `uniqueidentifier` | PK |
| `Name` | `nvarchar(200)` | NOT NULL |
| `LocationId` | `uniqueidentifier` | FK → Locations, NOT NULL |
| `StartDate` | `date` | NOT NULL |
| `EndDate` | `date` | NOT NULL |
| `CreatedByUserId` | `uniqueidentifier` | FK → Users, NOT NULL |
| `CreatedAt` | `datetimeoffset` | NOT NULL |
| `UpdatedAt` | `datetimeoffset` | NOT NULL |

Check constraint: `EndDate >= StartDate`.

### Attendances
| Column | Type | Constraints |
| --- | --- | --- |
| `OffCycleId` | `uniqueidentifier` | FK → OffCycles, NOT NULL, ON DELETE CASCADE |
| `UserId` | `uniqueidentifier` | FK → Users, NOT NULL |
| `Status` | `tinyint` | NOT NULL (enum) |
| `UpdatedAt` | `datetimeoffset` | NOT NULL |

**Composite PK** (`OffCycleId`, `UserId`) — one status per user per off-cycle.

## Enum encodings

- `Confidence`: `0 = Low`, `1 = Medium`, `2 = High`.
- `AttendanceStatus`: `0 = Interested`, `1 = CantMakeIt`, `2 = ProbablyComing`,
  `3 = DefinitelyComing`, `4 = Booked`.

## Query notes (performance)

- Interest counts / consensus sort: `GROUP BY LocationId` over `Interests`, read-only.
- Attendance summary: `GROUP BY Status` over `Attendances` for an off-cycle.
- All read queries `AsNoTracking`; collection includes use `AsSplitQuery`.
- No application-side joins; keep aggregation in SQL.

## Migrations & seeding

- Migrations live in `CycleSync.Api` (or a dedicated `CycleSync.Infrastructure`), applied by
  `CycleSync.MigrationService` at startup; the API waits for completion.
- Reference/seed data (currencies, country codes) seeded idempotently in the migration service.
- Integration/BDD tests run migrations against a Testcontainers SQL Server instance.
