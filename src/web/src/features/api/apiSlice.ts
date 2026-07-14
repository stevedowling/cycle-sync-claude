import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';
import type { RootState } from '../../app/store';
import type {
  LocationIntelligence,
  LocationResponse,
  LocationSearchResult,
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
  tagTypes: ['Locations'],
  endpoints: (builder) => ({
    signIn: builder.mutation<SignInResponse, { idToken: string }>({
      query: (body) => ({ url: '/auth/google', method: 'POST', body }),
    }),
    getLocations: builder.query<LocationResponse[], { sort?: 'interest' } | void>({
      query: (arg) => (arg && arg.sort ? `/locations?sort=${arg.sort}` : '/locations'),
      providesTags: ['Locations'],
    }),
    markInterest: builder.mutation<void, string>({
      query: (id) => ({ url: `/locations/${id}/interest`, method: 'PUT' }),
      invalidatesTags: ['Locations'],
    }),
    removeInterest: builder.mutation<void, string>({
      query: (id) => ({ url: `/locations/${id}/interest`, method: 'DELETE' }),
      invalidatesTags: ['Locations'],
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
  }),
});

export const {
  useSignInMutation,
  useGetLocationsQuery,
  useLazySearchLocationsQuery,
  usePersistLocationMutation,
  useMarkInterestMutation,
  useRemoveInterestMutation,
  useGetIntelligenceQuery,
} = apiSlice;
