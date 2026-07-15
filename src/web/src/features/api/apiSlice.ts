import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';
import type { RootState } from '../../app/store';
import type {
  AttendanceSummary,
  CostEstimate,
  CreateOffCycleRequest,
  LocationIntelligence,
  LocationResponse,
  LocationSearchResult,
  OffCycleResponse,
  SignInResponse,
} from '../../app/types';

export const apiSlice = createApi({
  reducerPath: 'api',
  baseQuery: fetchBaseQuery({
    // Absolute (same-origin) base so requests resolve in the browser and under jsdom alike; the
    // Vite dev proxy / production host serves /api on this origin.
    baseUrl: typeof window !== 'undefined' ? `${window.location.origin}/api` : '/api',
    prepareHeaders: (headers, { getState }) => {
      const token = (getState() as RootState).auth.token;
      if (token) {
        headers.set('Authorization', `Bearer ${token}`);
      }
      return headers;
    },
  }),
  tagTypes: ['Locations', 'OffCycles', 'Attendance'],
  endpoints: (builder) => ({
    signIn: builder.mutation<SignInResponse, { idToken: string }>({
      query: (body) => ({ url: '/auth/google', method: 'POST', body }),
    }),
    getLocations: builder.query<LocationResponse[], void>({
      query: () => '/locations',
      providesTags: ['Locations'],
    }),
    searchLocations: builder.query<LocationSearchResult[], string>({
      query: (q) => `/locations/search?q=${encodeURIComponent(q)}`,
    }),
    persistLocation: builder.mutation<LocationResponse, LocationSearchResult>({
      query: (result) => ({
        url: '/locations',
        method: 'POST',
        body: {
          name: result.name,
          country: result.country,
          latitude: result.coordinates.latitude,
          longitude: result.coordinates.longitude,
          azureMapsId: result.azureMapsId,
        },
      }),
      invalidatesTags: ['Locations'],
    }),
    getIntelligence: builder.query<LocationIntelligence, string>({
      query: (id) => `/locations/${id}/intelligence`,
    }),
    getOffCycles: builder.query<OffCycleResponse[], void>({
      query: () => '/off-cycles',
      providesTags: ['OffCycles'],
    }),
    createOffCycle: builder.mutation<OffCycleResponse, CreateOffCycleRequest>({
      query: (body) => ({ url: '/off-cycles', method: 'POST', body }),
      invalidatesTags: ['OffCycles'],
    }),
    getAttendance: builder.query<AttendanceSummary, string>({
      query: (id) => `/off-cycles/${id}/attendance`,
      providesTags: (_result, _error, id) => [{ type: 'Attendance', id }],
    }),
    setAttendance: builder.mutation<void, { id: string; status: string }>({
      query: ({ id, status }) => ({
        url: `/off-cycles/${id}/attendance`,
        method: 'PUT',
        body: { status },
      }),
      invalidatesTags: (_result, _error, { id }) => [{ type: 'Attendance', id }],
    }),
    getOffCycleCostEstimate: builder.query<CostEstimate, string>({
      query: (id) => `/off-cycles/${id}/cost-estimate`,
    }),
  }),
});

export const {
  useSignInMutation,
  useGetLocationsQuery,
  useLazySearchLocationsQuery,
  usePersistLocationMutation,
  useGetIntelligenceQuery,
  useGetOffCyclesQuery,
  useCreateOffCycleMutation,
  useGetAttendanceQuery,
  useSetAttendanceMutation,
  useGetOffCycleCostEstimateQuery,
} = apiSlice;
