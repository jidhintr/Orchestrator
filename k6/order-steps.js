import http from 'k6/http';
import { check, sleep } from 'k6';
import { client, login } from './lib/auth.js';
import { BASE_URL, buildOptions } from './lib/config.js';

// Setup creates one client and one catalog with no approval stage, so each placed order starts at once; iterations leave completed orders behind.
export const options = buildOptions();

// The intentional 404 and 409 checks below are expected responses, not failures.
http.setResponseCallback(http.expectedStatuses({ min: 200, max: 299 }, 404, 409));

const API = `${BASE_URL}/api/v1`;
const json = { headers: { 'Content-Type': 'application/json' } };

export function setup() {
  const token = login();
  const api = client(token);
  const suffix = Date.now().toString(36).toUpperCase();
  const clientRes = api.post(`${API}/clients`, JSON.stringify({
    companyName: `K6 Steps ${suffix}`,
    contactName: 'K6 Contact',
    email: `k6steps${suffix}@example.com`,
    phone: '+15551234567',
    shippingAddress: '1 Main St',
  }), json);
  const catalogRes = api.post(`${API}/catalogs`, JSON.stringify({ sku: `K6STP${suffix}`, name: `K6 Steps ${suffix}`, jobTypeIds: [1, 2], standardMinutes: [30, null] }), json);
  const approveRes = catalogRes.status === 201 ? api.post(`${API}/catalogs/${catalogRes.json('id')}/approve`) : catalogRes;
  if (clientRes.status !== 201 || catalogRes.status !== 201 || approveRes.status !== 200) {
    throw new Error(`Setup failed: client ${clientRes.status}, catalog ${catalogRes.status}, approve ${approveRes.status}.`);
  }
  return { token, clientId: clientRes.json('id'), catalogId: catalogRes.json('id') };
}

export default function ({ token, clientId, catalogId }) {
  const api = client(token);

  const placed = api.post(`${API}/orders`, JSON.stringify({ clientId, items: [{ catalogId, quantity: 1 }] }), json);
  if (!check(placed, { 'place: 201': (r) => r.status === 201, 'place: started at once': (r) => r.json('status') === 'InProgress' })) {
    return;
  }
  const first = placed.json('items.0.steps.0.id');
  const second = placed.json('items.0.steps.1.id');

  check(api.get(`${API}/order-steps?status=Ready`), { 'queue: 200': (r) => r.status === 200 });
  check(api.post(`${API}/order-steps/${second}/start`), { 'waiting step cannot start: 409': (r) => r.status === 409 });
  check(api.post(`${API}/order-steps/${first}/start`), { 'start: 200': (r) => r.status === 200 && r.json('status') === 'InProgress' });
  check(api.post(`${API}/order-steps/${first}/progress`, JSON.stringify({ progress: 50 }), json), { 'progress: 200': (r) => r.status === 200 });
  check(api.post(`${API}/order-steps/${first}/hold`, JSON.stringify({ reason: 'k6 hold' }), json), { 'hold: 200': (r) => r.status === 200 });
  check(api.post(`${API}/order-steps/${first}/resume`), { 'resume: 200': (r) => r.status === 200 });
  check(api.post(`${API}/order-steps/${first}/complete`), { 'complete: 200': (r) => r.status === 200 && r.json('progress') === 100 });
  check(api.get(`${API}/order-steps/${second}`), { 'next step ready: 200': (r) => r.status === 200 && r.json('status') === 'Ready' });
  check(api.post(`${API}/order-steps/${second}/start`), { 'start second: 200': (r) => r.status === 200 });
  check(api.post(`${API}/order-steps/${second}/reject`, JSON.stringify({ reason: 'k6 rework' }), json), { 'reject: 200': (r) => r.status === 200 && r.json('status') === 'Waiting' });
  check(api.post(`${API}/order-steps/${first}/start`), { 'reopened step starts: 200': (r) => r.status === 200 });
  check(api.post(`${API}/order-steps/${first}/complete`), { 'complete again: 200': (r) => r.status === 200 });
  check(api.post(`${API}/order-steps/${second}/start`), { 'restart second: 200': (r) => r.status === 200 });
  check(api.post(`${API}/order-steps/${second}/complete`), { 'complete second: 200': (r) => r.status === 200 });
  check(api.get(`${API}/orders/${placed.json('id')}`), { 'order completed': (r) => r.json('status') === 'Completed' });
  check(api.get(`${API}/order-steps/${first}/logs`), { 'logs: 200': (r) => r.status === 200 && r.json().length >= 6 });
  check(api.get(`${API}/order-steps/999999`), { 'unknown step: 404': (r) => r.status === 404 });

  sleep(1);
}
