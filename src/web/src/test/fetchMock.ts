import { vi } from 'vitest';

export interface MockRoute {
  method: string;
  /** Matches against the request path + query, e.g. "/api/locations/search?q=Lisbon". */
  match: (pathAndQuery: string) => boolean;
  respond: (body: unknown) => { status?: number; json?: unknown };
}

/**
 * Installs a deterministic `fetch` stand-in that routes requests to the first matching handler.
 * Returns the underlying mock so tests can assert on calls.
 */
export function installFetchMock(routes: MockRoute[]) {
  const mock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const request = input instanceof Request ? input : new Request(input, init);
    const method = request.method.toUpperCase();
    const url = new URL(request.url, 'http://localhost');
    const pathAndQuery = url.pathname + url.search;

    let requestBody: unknown;
    if (method !== 'GET' && method !== 'HEAD') {
      const text = await request.clone().text();
      requestBody = text ? JSON.parse(text) : undefined;
    }

    const route = routes.find((r) => r.method.toUpperCase() === method && r.match(pathAndQuery));
    if (!route) {
      return new Response(JSON.stringify({ detail: 'no mock route' }), { status: 404 });
    }

    const { status = 200, json } = route.respond(requestBody);
    return new Response(json === undefined ? null : JSON.stringify(json), {
      status,
      headers: { 'Content-Type': 'application/json' },
    });
  });

  vi.stubGlobal('fetch', mock);
  return mock;
}
