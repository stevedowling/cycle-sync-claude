export interface UserSummary {
  id: string;
  email: string;
  displayName: string;
}

export interface SignInResponse {
  token: string;
  user: UserSummary;
}

export interface Coordinates {
  latitude: number;
  longitude: number;
}

export interface LocationSearchResult {
  name: string;
  country: string;
  coordinates: Coordinates;
  azureMapsId: string | null;
}

export interface LocationResponse {
  id: string;
  name: string;
  country: string;
  coordinates: Coordinates;
  createdAt: string;
}

export interface LocationIntelligence {
  locationId: string;
  climateSummary: string | null;
  bestTimesToVisit: string | null;
  travelTips: string | null;
  visaNotes: string | null;
  confidence: string;
  generatedAt: string;
}

export interface OffCycleResponse {
  id: string;
  name: string;
  locationId: string;
  locationName: string;
  startDate: string;
  endDate: string;
  nights: number;
  createdByUserId: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateOffCycleRequest {
  name: string;
  locationId: string;
  startDate: string;
  endDate: string;
}

export interface AttendanceRosterEntry {
  userId: string;
  displayName: string;
  status: string;
}

export interface AttendanceSummary {
  offCycleId: string;
  counts: Record<string, number>;
  roster: AttendanceRosterEntry[];
}

export interface CostEstimate {
  currency: string;
  flights: number;
  accommodation: number;
  dailyExpenses: number;
  nights: number;
  confidence: string;
  generatedAt: string;
}
