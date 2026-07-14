import { afterEach, describe, expect, it, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { installFetchMock } from '../../test/fetchMock';
import { renderWithProviders, signedInState } from '../../test/renderWithProviders';
import { LocationsPage } from './LocationsPage';

const lisbon = {
  name: 'Lisbon, Portugal',
  country: 'Portugal',
  coordinates: { latitude: 38.7223, longitude: -9.1393 },
  azureMapsId: 'PT/Lisbon',
};

const savedLisbon = {
  id: 'loc-1',
  name: 'Lisbon, Portugal',
  country: 'Portugal',
  coordinates: { latitude: 38.7223, longitude: -9.1393 },
  createdAt: '2026-07-14T00:00:00Z',
  interestCount: 2,
  isInterested: false,
};

const savedTallinn = {
  id: 'loc-2',
  name: 'Tallinn, Estonia',
  country: 'Estonia',
  coordinates: { latitude: 59.437, longitude: 24.7536 },
  createdAt: '2026-07-14T00:00:00Z',
  interestCount: 1,
  isInterested: false,
};

afterEach(() => vi.unstubAllGlobals());

describe('LocationsPage', () => {
  it('searches Azure Maps and shows matching destinations with a country', async () => {
    installFetchMock([
      { method: 'GET', match: (p) => p === '/api/locations', respond: () => ({ json: [] }) },
      {
        method: 'GET',
        match: (p) => p.startsWith('/api/locations/search'),
        respond: () => ({ json: [lisbon] }),
      },
    ]);

    renderWithProviders(<LocationsPage />, { preloadedState: signedInState() });

    await userEvent.type(screen.getByTestId('search-input'), 'Lisbon');
    await userEvent.click(screen.getByTestId('search-submit'));

    const results = await screen.findByTestId('search-results');
    expect(results).toHaveTextContent('Lisbon, Portugal');
    expect(results).toHaveTextContent('Portugal');
  });

  it('persists the selected result via POST /api/locations', async () => {
    const mock = installFetchMock([
      { method: 'GET', match: (p) => p === '/api/locations', respond: () => ({ json: [] }) },
      {
        method: 'GET',
        match: (p) => p.startsWith('/api/locations/search'),
        respond: () => ({ json: [lisbon] }),
      },
      {
        method: 'POST',
        match: (p) => p === '/api/locations',
        respond: () => ({
          status: 201,
          json: { id: 'loc-1', name: lisbon.name, country: lisbon.country, coordinates: lisbon.coordinates, createdAt: '2026-07-14T00:00:00Z' },
        }),
      },
    ]);

    renderWithProviders(<LocationsPage />, { preloadedState: signedInState() });

    await userEvent.type(screen.getByTestId('search-input'), 'Lisbon');
    await userEvent.click(screen.getByTestId('search-submit'));
    await userEvent.click(await screen.findByTestId('select-Lisbon, Portugal'));

    await waitFor(() => {
      const postCall = mock.mock.calls.find(([input, init]) => {
        const req = input instanceof Request ? input : new Request(input as string, init);
        return req.method === 'POST' && req.url.endsWith('/api/locations');
      });
      expect(postCall).toBeTruthy();
    });
  });

  it('shows the interest count and marks interest via PUT', async () => {
    const mock = installFetchMock([
      { method: 'GET', match: (p) => p === '/api/locations', respond: () => ({ json: [savedLisbon] }) },
      {
        method: 'PUT',
        match: (p) => p === '/api/locations/loc-1/interest',
        respond: () => ({ status: 204 }),
      },
    ]);

    renderWithProviders(<LocationsPage />, { preloadedState: signedInState() });

    expect(await screen.findByTestId('interest-count-Lisbon, Portugal')).toHaveTextContent('2 interested');

    await userEvent.click(screen.getByTestId('interest-toggle-Lisbon, Portugal'));

    await waitFor(() => {
      const putCall = mock.mock.calls.find(([input, init]) => {
        const req = input instanceof Request ? input : new Request(input as string, init);
        return req.method === 'PUT' && req.url.endsWith('/api/locations/loc-1/interest');
      });
      expect(putCall).toBeTruthy();
    });
  });

  it('requests consensus order when sorting by interest', async () => {
    const mock = installFetchMock([
      {
        method: 'GET',
        match: (p) => p.startsWith('/api/locations?sort=interest'),
        respond: () => ({ json: [savedLisbon, savedTallinn] }),
      },
      { method: 'GET', match: (p) => p === '/api/locations', respond: () => ({ json: [savedTallinn, savedLisbon] }) },
    ]);

    renderWithProviders(<LocationsPage />, { preloadedState: signedInState() });

    await screen.findByTestId('location-list');
    await userEvent.click(screen.getByTestId('sort-by-interest'));

    await waitFor(() => {
      const sortCall = mock.mock.calls.find(([input, init]) => {
        const req = input instanceof Request ? input : new Request(input as string, init);
        return req.method === 'GET' && req.url.includes('/api/locations?sort=interest');
      });
      expect(sortCall).toBeTruthy();
    });

    // The consensus-ordered list (server-sorted) places the higher-interest destination first.
    await waitFor(() => {
      const items = screen.getAllByTestId(/^location-(Lisbon|Tallinn)/);
      expect(items[0]).toHaveTextContent('Lisbon, Portugal');
    });
  });
});
