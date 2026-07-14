import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { installFetchMock } from './test/fetchMock';
import { renderWithProviders } from './test/renderWithProviders';
import App from './App';

beforeEach(() => localStorage.clear());
afterEach(() => vi.unstubAllGlobals());

describe('App authentication gate', () => {
  it('shows the sign-in form when there is no session', () => {
    installFetchMock([]);
    renderWithProviders(<App />);
    expect(screen.getByTestId('signin-email')).toBeInTheDocument();
  });

  it('signs in and reveals the locations screen', async () => {
    installFetchMock([
      {
        method: 'POST',
        match: (p) => p === '/api/auth/google',
        respond: (body) => ({
          json: {
            token: 'jwt-123',
            user: { id: 'user-1', email: (body as { idToken: string }).idToken, displayName: 'Amara' },
          },
        }),
      },
      { method: 'GET', match: (p) => p === '/api/locations', respond: () => ({ json: [] }) },
    ]);

    renderWithProviders(<App />);

    await userEvent.type(screen.getByTestId('signin-email'), 'amara@cyclesync.example');
    await userEvent.click(screen.getByTestId('signin-submit'));

    expect(await screen.findByTestId('current-user')).toHaveTextContent('Amara');
    expect(screen.getByTestId('search-input')).toBeInTheDocument();
  });
});
