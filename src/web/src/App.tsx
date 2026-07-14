import { useEffect, useState } from 'react';
import './App.css';

interface PingResponse {
  service: string;
  status: string;
}

type ConnectionState =
  | { kind: 'loading' }
  | { kind: 'connected'; service: string }
  | { kind: 'error'; message: string };

function App() {
  const [connection, setConnection] = useState<ConnectionState>({ kind: 'loading' });

  useEffect(() => {
    let cancelled = false;

    const ping = async () => {
      try {
        const response = await fetch('/api/ping');
        if (!response.ok) {
          throw new Error(`API responded ${response.status}`);
        }
        const data: PingResponse = await response.json();
        if (!cancelled) {
          setConnection({ kind: 'connected', service: data.service });
        }
      } catch (err) {
        if (!cancelled) {
          setConnection({
            kind: 'error',
            message: err instanceof Error ? err.message : 'Unable to reach the API',
          });
        }
      }
    };

    void ping();
    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <div className="app-shell" data-testid="app-shell">
      <header className="app-header">
        <h1 className="app-title">CycleSync</h1>
        <p className="app-subtitle">
          Decide where your distributed team should meet.
        </p>
      </header>

      <main className="main-content">
        <section className="card" aria-labelledby="status-heading">
          <h2 id="status-heading" className="section-title">
            System status
          </h2>

          {connection.kind === 'loading' && (
            <p role="status" aria-live="polite" data-testid="api-status">
              Connecting to the CycleSync API…
            </p>
          )}

          {connection.kind === 'connected' && (
            <p className="status-ok" data-testid="api-status">
              Connected to <strong>{connection.service}</strong> API.
            </p>
          )}

          {connection.kind === 'error' && (
            <p className="status-error" role="alert" data-testid="api-status">
              {connection.message}
            </p>
          )}
        </section>
      </main>

      <footer className="app-footer">
        <span>CycleSync · Phase 0 walking skeleton</span>
      </footer>
    </div>
  );
}

export default App;
