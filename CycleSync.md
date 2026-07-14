# CycleSync

CycleSync helps globally distributed teams decide where to meet by aggregating location preferences, providing travel intelligence, and facilitating off-cycle event planning.

## Overview

CycleSync solves the coordination problem of planning team meetups across multiple countries by providing:

* **Location Discovery**: Search and explore potential meetup destinations
* **Location Intelligence**: Climate data, visa requirements, cost estimates, travel times
* **Interest Tracking**: See which locations your teammates are interested in
* **Off-Cycle Planning**: Create and manage specific meetup events with dates and attendance tracking
* **Cost Transparency**: Understand travel and accommodation costs for different locations

## Core Concepts

### Locations

Persistent destinations that users can search for and express interest in. Each location includes:

* Climate and travel intelligence
* Visa requirements (based on user's passport)
* Cost estimates (flights, accommodation, daily expenses)
* Best times to visit

### Interest

Users can mark locations they're interested in visiting. Interest counts help teams identify consensus destinations.

### Off-Cycles

Planned meetup events with:

* Specific location and dates
* Per-user attendance status tracking
* Recalculated cost estimates based on actual dates

### Attendance Status

* **Interested**: Considering attending
* **Can't Make It**: Unable to attend these dates
* **Probably Coming**: Likely to attend
* **Definitely Coming**: Committed to attending
* **Booked**: Travel and accommodation confirmed

## Key Features

### MVP Features

* ✅ Google OAuth authentication (domain-restricted)
* ✅ User profile management (location, currency, language, passports)
* ✅ Location search via Azure Maps
* ✅ AI-generated location intelligence (climate, travel tips)
* ✅ Interest tracking and sorting
* ✅ Off-cycle creation and management
* ✅ Attendance status tracking
* ✅ Cost estimation (heuristic model)

### Future Enhancements

* Replace AI generated local intelligence with actual data, where possible
* Slack integration (link to off-cycle channel)
* Real-time flight pricing via API
* Real-time flight tracking so people involved in an off-cycle can see when others are arriving/leaving
* Enhanced visa requirements via dedicated API
* Guest accounts (view-only, linked to users)
* Budget pool tracking
* Time zone overlap scoring
* Mobile app (MAUI)

  * Expense tracking - push to Mesh

## Design Principles

1. **Equal Access**: All authenticated users have the same rights; no system administrators
2. **Transparent Costs**: Always show estimate confidence and generation timestamps
3. **Privacy-Friendly**: Users already share home location; all data visible to all users
4. **Location Permanence**: Locations never deleted
