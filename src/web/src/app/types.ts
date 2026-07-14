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
  interestCount: number;
  isInterested: boolean;
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
