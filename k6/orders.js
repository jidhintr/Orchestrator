import http from 'k6/http';
import { check, sleep } from 'k6';
import { client, login } from './lib/auth.js';
import { BASE_URL, buildOptions } from './lib/config.js';

// Setup creates one client and one catalog; each iteration places an order for them and cancels it, so runs leave rows behind in the target database.
export const options = buildOptions();

// The intentional 400, 404 and 409 checks below are expected responses, not failures.
http.setResponseCallback(http.expectedStatuses({ min: 200, max: 299 }, 400, 404, 409));

const API = `${BASE_URL}/api/v1`;
const json = { headers: { 'Content-Type': 'application/json' } };

export function setup() {
  const token = login();
  const api = client(token);
  const suffix = Date.now().toString(36).toUpperCase();
  const clientRes = api.post(`${API}/clients`, JSON.stringify({
    companyName: `K6 Orders ${suffix}`,
    contactName: 'K6 Contact',
    email: `k6orders${suffix}@example.com`,
    phone: '+15551234567',
    shippingAddress: '1 Main St',
  }), json);
  const catalogRes = api.post(`${API}/catalogs`, JSON.stringify({ sku: `K6ORD${suffix}`, name: `K6 Orders ${suffix}`, jobTypeIds: [1, 2, 4] }), json);
  if (clientRes.status !== 201 || catalogRes.status !== 201) {
    throw new Error(`Setup failed: client ${clientRes.status}, catalog ${catalogRes.status}.`);
  }
  return { token, clientId: clientRes.json('id'), catalogId: catalogRes.json('id') };
}

export default function ({ token, clientId, catalogId }) {
  const api = client(token);

  check(api.get(`${API}/orders`), { 'list: 200': (r) => r.status === 200 });

  const placed = api.post(`${API}/orders`, JSON.stringify({ clientId, catalogId, quantity: 5, priority: 'High' }), json);
  if (!check(placed, {
    'place: 201': (r) => r.status === 201,
    'place: pending approval': (r) => r.json('status') === 'PendingApproval',
    'place: three stages': (r) => r.json('stages').length === 3,
    'place: quoted': (r) => r.json('quotedTotal') >= 0 && r.json('quantity') === 5,
  })) {
    return;
  }
  const id = placed.json('id');

  check(api.get(`${API}/orders/${id}`), { 'get: 200': (r) => r.status === 200 });
  check(api.get(`${API}/orders/${id}/events`), { 'events: placed': (r) => r.status === 200 && r.json('0.type') === 'Placed' });
  check(api.post(`${API}/orders/${id}/quote`, JSON.stringify({ total: 1, reason: 'k6 quote' }), json), { 'quote: 200': (r) => r.status === 200 && r.json('quotedTotal') === 1 });
  check(api.post(`${API}/orders/${id}/priority-requests`, JSON.stringify({ priority: 'Premium' }), json), { 'priority request: 201': (r) => r.status === 201 });
  check(api.post(`${API}/orders/${id}/priority-requests`, JSON.stringify({ priority: 'High' }), json), { 'same priority: 400': (r) => r.status === 400 });
  check(api.post(`${API}/orders/${id}/cancel`, JSON.stringify({ reason: 'k6 cleanup' }), json), {
    'cancel: 200': (r) => r.status === 200,
    'cancel: cancelled': (r) => r.json('status') === 'Cancelled',
  });
  check(api.post(`${API}/orders/${id}/cancel`, JSON.stringify({ reason: 'k6 again' }), json), { 'cancel twice: 409': (r) => r.status === 409 });
  check(api.get(`${API}/orders/999999`), { 'unknown id: 404': (r) => r.status === 404 });

  sleep(1);
}
