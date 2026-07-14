import { afterEach, describe, expect, it, vi } from 'vitest';
import { screen } from '@testing-library/react';
import { Route, Routes } from 'react-router-dom';
import { installFetchMock } from '../../test/fetchMock';
import { renderWithProviders, signedInState } from '../../test/renderWithProviders';
import { LocationDetailPage } from './LocationDetailPage';

const intelligence = {
  locationId: 'loc-1',
  climateSummary: 'Lisbon, Portugal has a generally temperate climate.',
  bestTimesToVisit: 'Spring and early autumn are most comfortable.',
  travelTips: 'Public transport is reliable across Portugal.',
  visaNotes: 'Visa guidance for New Zealand passport holders travelling to Portugal.',
  confidence: 'Low',
  generatedAt: '2026-07-13T00:00:00Z',
};

afterEach(() => vi.unstubAllGlobals());

describe('LocationDetailPage', () => {
  it('shows AI intelligence with confidence, timestamp and passport-aware visa guidance', async () => {
    installFetchMock([
      {
        method: 'GET',
        match: (p) => p === '/api/locations/loc-1/intelligence',
        respond: () => ({ json: intelligence }),
      },
    ]);

    renderWithProviders(
      <Routes>
        <Route path="/locations/:id" element={<LocationDetailPage />} />
      </Routes>,
      { preloadedState: signedInState(), route: '/locations/loc-1' },
    );

    expect(await screen.findByTestId('intel-climate')).toHaveTextContent('temperate climate');
    expect(screen.getByTestId('intel-best-times')).toHaveTextContent('Spring');
    expect(screen.getByTestId('intel-visa')).toHaveTextContent('New Zealand');
    // Transparency: confidence and freshness are always disclosed.
    expect(screen.getByTestId('intel-confidence')).toHaveTextContent('Low');
    expect(screen.getByTestId('intel-generated')).toHaveTextContent('Generated');
  });
});
