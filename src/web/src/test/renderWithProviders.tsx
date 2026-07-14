import type { ReactElement, ReactNode } from 'react';
import { render } from '@testing-library/react';
import { Provider } from 'react-redux';
import { MemoryRouter } from 'react-router-dom';
import { setupStore, type AppStore, type RootState } from '../app/store';

interface RenderOptions {
  preloadedState?: Partial<RootState>;
  store?: AppStore;
  route?: string;
}

/** Renders a component inside a fresh store and router so each test is isolated. */
export function renderWithProviders(
  ui: ReactElement,
  { preloadedState, store = setupStore(preloadedState), route = '/' }: RenderOptions = {},
) {
  function Wrapper({ children }: { children: ReactNode }) {
    return (
      <Provider store={store}>
        <MemoryRouter initialEntries={[route]}>{children}</MemoryRouter>
      </Provider>
    );
  }

  return { store, ...render(ui, { wrapper: Wrapper }) };
}

/** An auth slice preloaded with a signed-in session. */
export function signedInState(): Partial<RootState> {
  return {
    auth: {
      token: 'test-token',
      user: { id: 'user-1', email: 'amara@cyclesync.example', displayName: 'Amara' },
    },
  };
}
