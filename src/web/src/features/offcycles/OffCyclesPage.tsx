import { useMemo, useState, type FormEvent } from 'react';
import {
  useCreateOffCycleMutation,
  useGetAttendanceQuery,
  useGetLocationsQuery,
  useGetOffCyclesQuery,
  useSetAttendanceMutation,
} from '../api/apiSlice';
import type { OffCycleResponse } from '../../app/types';

/** The five attendance statuses, in progression order (display labels match the API contract). */
const ATTENDANCE_STATUSES = [
  'Interested',
  "Can't Make It",
  'Probably Coming',
  'Definitely Coming',
  'Booked',
];

export function OffCyclesPage() {
  const { data: locations = [] } = useGetLocationsQuery();
  const { data: offCycles = [], isLoading } = useGetOffCyclesQuery();
  const [createOffCycle, { isLoading: isCreating, error: createError }] = useCreateOffCycleMutation();

  const [name, setName] = useState('');
  const [locationId, setLocationId] = useState('');
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');

  // Default the location select to the first saved location once they load.
  const effectiveLocationId = locationId || locations[0]?.id || '';

  const onCreate = async (event: FormEvent) => {
    event.preventDefault();
    if (!name.trim() || !effectiveLocationId || !startDate || !endDate) {
      return;
    }
    await createOffCycle({ name: name.trim(), locationId: effectiveLocationId, startDate, endDate }).unwrap();
    setName('');
    setStartDate('');
    setEndDate('');
  };

  return (
    <div className="off-cycles">
      <section className="card" aria-labelledby="create-heading">
        <h2 id="create-heading" className="section-title">
          Plan an off-cycle
        </h2>
        <form onSubmit={onCreate} className="off-cycle-form">
          <label htmlFor="oc-name">Name</label>
          <input
            id="oc-name"
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="e.g. Autumn Meetup"
            data-testid="offcycle-name"
          />

          <label htmlFor="oc-location">Location</label>
          <select
            id="oc-location"
            value={effectiveLocationId}
            onChange={(e) => setLocationId(e.target.value)}
            data-testid="offcycle-location"
          >
            {locations.length === 0 && <option value="">No saved locations yet</option>}
            {locations.map((location) => (
              <option key={location.id} value={location.id}>
                {location.name}
              </option>
            ))}
          </select>

          <label htmlFor="oc-start">Start date</label>
          <input
            id="oc-start"
            type="date"
            value={startDate}
            onChange={(e) => setStartDate(e.target.value)}
            data-testid="offcycle-start"
          />

          <label htmlFor="oc-end">End date</label>
          <input
            id="oc-end"
            type="date"
            value={endDate}
            onChange={(e) => setEndDate(e.target.value)}
            data-testid="offcycle-end"
          />

          <button type="submit" disabled={isCreating || locations.length === 0} data-testid="offcycle-create">
            {isCreating ? 'Creating…' : 'Create off-cycle'}
          </button>

          {createError && (
            <p role="alert" className="error" data-testid="offcycle-error">
              Could not create the off-cycle. Check the dates and try again.
            </p>
          )}
        </form>
      </section>

      <section className="card" aria-labelledby="list-heading">
        <h2 id="list-heading" className="section-title">
          Planned off-cycles
        </h2>
        {isLoading ? (
          <p role="status">Loading off-cycles…</p>
        ) : offCycles.length === 0 ? (
          <p className="muted">No off-cycles yet — plan one above.</p>
        ) : (
          <ul className="off-cycle-list" data-testid="offcycle-list">
            {offCycles.map((offCycle) => (
              <OffCycleRow key={offCycle.id} offCycle={offCycle} />
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}

function OffCycleRow({ offCycle }: { offCycle: OffCycleResponse }) {
  const { data: attendance } = useGetAttendanceQuery(offCycle.id);
  const [setAttendance, { isLoading: isSaving }] = useSetAttendanceMutation();

  const countsSummary = useMemo(() => {
    if (!attendance) {
      return '';
    }
    return Object.entries(attendance.counts)
      .map(([status, count]) => `${count} ${status}`)
      .join(' · ');
  }, [attendance]);

  return (
    <li className="off-cycle-item" data-testid={`offcycle-${offCycle.name}`}>
      <div>
        <strong>{offCycle.name}</strong>
        <span className="muted"> · {offCycle.locationName}</span>
        <div className="muted">
          {offCycle.startDate} → {offCycle.endDate} ({offCycle.nights} nights)
        </div>
        {countsSummary && (
          <div className="muted" data-testid={`attendance-summary-${offCycle.id}`}>
            {countsSummary}
          </div>
        )}
      </div>
      <label className="visually-hidden" htmlFor={`attendance-${offCycle.id}`}>
        My attendance for {offCycle.name}
      </label>
      <select
        id={`attendance-${offCycle.id}`}
        disabled={isSaving}
        defaultValue=""
        onChange={(e) => setAttendance({ id: offCycle.id, status: e.target.value })}
        data-testid={`attendance-${offCycle.id}`}
      >
        <option value="" disabled>
          Set attendance…
        </option>
        {ATTENDANCE_STATUSES.map((status) => (
          <option key={status} value={status}>
            {status}
          </option>
        ))}
      </select>
    </li>
  );
}
