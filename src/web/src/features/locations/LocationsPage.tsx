import { useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import {
  useGetLocationsQuery,
  useLazySearchLocationsQuery,
  usePersistLocationMutation,
} from '../api/apiSlice';
import type { LocationSearchResult } from '../../app/types';

export function LocationsPage() {
  const [query, setQuery] = useState('');
  const [runSearch, searchState] = useLazySearchLocationsQuery();
  const [persistLocation, { isLoading: isPersisting }] = usePersistLocationMutation();
  const { data: locations = [], isLoading: isLoadingLocations } = useGetLocationsQuery();

  const onSearch = async (event: FormEvent) => {
    event.preventDefault();
    const trimmed = query.trim();
    if (trimmed) {
      await runSearch(trimmed);
    }
  };

  const onSelect = async (result: LocationSearchResult) => {
    await persistLocation(result).unwrap();
  };

  return (
    <div className="locations">
      <section className="card" aria-labelledby="search-heading">
        <h2 id="search-heading" className="section-title">
          Find a destination
        </h2>
        <form onSubmit={onSearch} className="search-form">
          <label htmlFor="q">Search cities</label>
          <input
            id="q"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="e.g. Lisbon"
            data-testid="search-input"
          />
          <button type="submit" disabled={searchState.isFetching} data-testid="search-submit">
            {searchState.isFetching ? 'Searching…' : 'Search'}
          </button>
        </form>

        {searchState.data && (
          <ul className="search-results" data-testid="search-results">
            {searchState.data.length === 0 && <li className="muted">No matches.</li>}
            {searchState.data.map((result) => (
              <li key={result.azureMapsId ?? result.name} className="search-result">
                <span>
                  <strong>{result.name}</strong>
                  <span className="muted"> · {result.country}</span>
                </span>
                <button
                  type="button"
                  onClick={() => onSelect(result)}
                  disabled={isPersisting}
                  data-testid={`select-${result.name}`}
                >
                  Add
                </button>
              </li>
            ))}
          </ul>
        )}
      </section>

      <section className="card" aria-labelledby="saved-heading">
        <h2 id="saved-heading" className="section-title">
          Saved destinations
        </h2>
        {isLoadingLocations ? (
          <p role="status">Loading destinations…</p>
        ) : locations.length === 0 ? (
          <p className="muted">No destinations yet — search and add one above.</p>
        ) : (
          <ul className="location-list" data-testid="location-list">
            {locations.map((location) => (
              <li key={location.id} className="location-item">
                <Link to={`/locations/${location.id}`} data-testid={`location-${location.name}`}>
                  {location.name}
                </Link>
                <span className="muted"> · {location.country}</span>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}
