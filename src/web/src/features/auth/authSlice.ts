import { createSlice, type PayloadAction } from '@reduxjs/toolkit';
import type { SignInResponse, UserSummary } from '../../app/types';

const TOKEN_KEY = 'cyclesync.token';
const USER_KEY = 'cyclesync.user';

interface AuthState {
  token: string | null;
  user: UserSummary | null;
}

function loadInitialState(): AuthState {
  try {
    const token = localStorage.getItem(TOKEN_KEY);
    const userJson = localStorage.getItem(USER_KEY);
    return {
      token,
      user: userJson ? (JSON.parse(userJson) as UserSummary) : null,
    };
  } catch {
    return { token: null, user: null };
  }
}

const authSlice = createSlice({
  name: 'auth',
  initialState: loadInitialState(),
  reducers: {
    credentialsReceived(state, action: PayloadAction<SignInResponse>) {
      state.token = action.payload.token;
      state.user = action.payload.user;
      try {
        localStorage.setItem(TOKEN_KEY, action.payload.token);
        localStorage.setItem(USER_KEY, JSON.stringify(action.payload.user));
      } catch {
        // Persistence is best-effort; the in-memory session still works.
      }
    },
    loggedOut(state) {
      state.token = null;
      state.user = null;
      try {
        localStorage.removeItem(TOKEN_KEY);
        localStorage.removeItem(USER_KEY);
      } catch {
        // ignore
      }
    },
  },
});

export const { credentialsReceived, loggedOut } = authSlice.actions;
export default authSlice.reducer;
