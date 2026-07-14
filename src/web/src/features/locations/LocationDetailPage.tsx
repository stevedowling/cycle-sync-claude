import { Link, useParams } from 'react-router-dom';
import { useGetIntelligenceQuery } from '../api/apiSlice';

function formatTimestamp(iso: string): string {
  const date = new Date(iso);
  return Number.isNaN(date.getTime()) ? iso : date.toLocaleString();
}

export function LocationDetailPage() {
  const { id = '' } = useParams();
  const { data: intelligence, isLoading, isError } = useGetIntelligenceQuery(id);

  return (
    <div className="location-detail">
      <p>
        <Link to="/" data-testid="back-link">
          ← Back to destinations
        </Link>
      </p>

      <section className="card" aria-labelledby="intel-heading">
        <h2 id="intel-heading" className="section-title">
          Location intelligence
        </h2>

        {isLoading && <p role="status">Generating intelligence…</p>}
        {isError && (
          <p className="status-error" role="alert">
            Could not load intelligence for this destination.
          </p>
        )}

        {intelligence && (
          <div data-testid="intelligence">
            {/* Transparency: always disclose confidence and freshness. */}
            <p className="intel-meta" data-testid="intel-meta">
              <span className="badge" data-testid="intel-confidence">
                Confidence: {intelligence.confidence}
              </span>
              <span className="muted" data-testid="intel-generated">
                Generated {formatTimestamp(intelligence.generatedAt)}
              </span>
            </p>

            <h3>Climate</h3>
            <p data-testid="intel-climate">{intelligence.climateSummary}</p>

            <h3>Best times to visit</h3>
            <p data-testid="intel-best-times">{intelligence.bestTimesToVisit}</p>

            <h3>Travel tips</h3>
            <p data-testid="intel-tips">{intelligence.travelTips}</p>

            <h3>Visa guidance</h3>
            <p data-testid="intel-visa">{intelligence.visaNotes}</p>
          </div>
        )}
      </section>
    </div>
  );
}
