import { Navigate, Route, Routes } from 'react-router-dom';
import { useAppDispatch, useAppSelector } from './app/hooks';
import { loggedOut } from './features/auth/authSlice';
import { SignIn } from './features/auth/SignIn';
import { LocationsPage } from './features/locations/LocationsPage';
import { LocationDetailPage } from './features/locations/LocationDetailPage';
import './App.css';

function App() {
  const dispatch = useAppDispatch();
  const { token, user } = useAppSelector((state) => state.auth);
  const isAuthenticated = Boolean(token);

  return (
    <div className="app-shell" data-testid="app-shell">
      <header className="app-header">
        <div>
          <h1 className="app-title">CycleSync</h1>
          <p className="app-subtitle">Decide where your distributed team should meet.</p>
        </div>
        {isAuthenticated && (
          <div className="app-user" data-testid="current-user">
            <span className="muted">{user?.displayName ?? user?.email}</span>
            <button type="button" onClick={() => dispatch(loggedOut())} data-testid="sign-out">
              Sign out
            </button>
          </div>
        )}
      </header>

      <main className="main-content">
        {!isAuthenticated ? (
          <SignIn />
        ) : (
          <Routes>
            <Route path="/" element={<LocationsPage />} />
            <Route path="/locations/:id" element={<LocationDetailPage />} />
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        )}
      </main>

      <footer className="app-footer">
        <span>CycleSync · Phase 2 — locations &amp; discovery</span>
      </footer>
    </div>
  );
}

export default App;
