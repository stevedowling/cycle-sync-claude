import { afterEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { installFetchMock } from '../../test/fetchMock';
import { renderWithProviders, signedInState } from '../../test/renderWithProviders';
import { OffCyclesPage } from './OffCyclesPage';

const lisbon = {
  id: 'loc-1',
  name: 'Lisbon, Portugal',
  country: 'Portugal',
  coordinates: { latitude: 38.7223, longitude: -9.1393 },
  createdAt: '2026-07-14T00:00:00Z',
};

const autumnMeetup = {
  id: 'oc-1',
  name: 'Autumn Meetup',
  locationId: 'loc-1',
  locationName: 'Lisbon, Portugal',
  startDate: '2026-10-05',
  endDate: '2026-10-09',
  nights: 4,
  createdByUserId: 'user-1',
  createdAt: '2026-07-14T00:00:00Z',
  updatedAt: '2026-07-14T00:00:00Z',
};

const attendance = {
  offCycleId: 'oc-1',
  counts: { Interested: 1 },
  roster: [{ userId: 'user-1', displayName: 'Amara', status: 'Interested' }],
};

afterEach(() => vi.unstubAllGlobals());

describe('OffCyclesPage', () => {
  it('lists off-cycles with their location and dates', async () => {
    installFetchMock([
      { method: 'GET', match: (p) => p === '/api/locations', respond: () => ({ json: [lisbon] }) },
      { method: 'GET', match: (p) => p === '/api/off-cycles', respond: () => ({ json: [autumnMeetup] }) },
      {
        method: 'GET',
        match: (p) => p === '/api/off-cycles/oc-1/attendance',
        respond: () => ({ json: attendance }),
      },
    ]);

    renderWithProviders(<OffCyclesPage />, { preloadedState: signedInState() });

    const row = await screen.findByTestId('offcycle-Autumn Meetup');
    expect(row).toHaveTextContent('Lisbon, Portugal');
    expect(row).toHaveTextContent('2026-10-05');
    expect(row).toHaveTextContent('2026-10-09');
    expect(await screen.findByTestId('attendance-summary-oc-1')).toHaveTextContent('1 Interested');
  });

  it('creates an off-cycle via POST /api/off-cycles', async () => {
    const mock = installFetchMock([
      { method: 'GET', match: (p) => p === '/api/locations', respond: () => ({ json: [lisbon] }) },
      { method: 'GET', match: (p) => p === '/api/off-cycles', respond: () => ({ json: [] }) },
      {
        method: 'POST',
        match: (p) => p === '/api/off-cycles',
        respond: () => ({ status: 201, json: autumnMeetup }),
      },
    ]);

    renderWithProviders(<OffCyclesPage />, { preloadedState: signedInState() });

    await userEvent.type(screen.getByTestId('offcycle-name'), 'Autumn Meetup');
    fireEvent.change(screen.getByTestId('offcycle-start'), { target: { value: '2026-10-05' } });
    fireEvent.change(screen.getByTestId('offcycle-end'), { target: { value: '2026-10-09' } });
    await userEvent.click(screen.getByTestId('offcycle-create'));

    await waitFor(() => {
      const postCall = mock.mock.calls.find(([input, init]) => {
        const req = input instanceof Request ? input : new Request(input as string, init);
        return req.method === 'POST' && req.url.endsWith('/api/off-cycles');
      });
      expect(postCall).toBeTruthy();
    });
  });

  it('sets my attendance via PUT /api/off-cycles/:id/attendance', async () => {
    const mock = installFetchMock([
      { method: 'GET', match: (p) => p === '/api/locations', respond: () => ({ json: [lisbon] }) },
      { method: 'GET', match: (p) => p === '/api/off-cycles', respond: () => ({ json: [autumnMeetup] }) },
      {
        method: 'GET',
        match: (p) => p === '/api/off-cycles/oc-1/attendance',
        respond: () => ({ json: attendance }),
      },
      {
        method: 'PUT',
        match: (p) => p === '/api/off-cycles/oc-1/attendance',
        respond: () => ({ status: 204 }),
      },
    ]);

    renderWithProviders(<OffCyclesPage />, { preloadedState: signedInState() });

    const select = await screen.findByTestId('attendance-oc-1');
    await userEvent.selectOptions(select, 'Booked');

    await waitFor(() => {
      const putCall = mock.mock.calls.find(([input, init]) => {
        const req = input instanceof Request ? input : new Request(input as string, init);
        return req.method === 'PUT' && req.url.endsWith('/api/off-cycles/oc-1/attendance');
      });
      expect(putCall).toBeTruthy();
    });
  });
});
