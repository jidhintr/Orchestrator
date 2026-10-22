import http from 'k6/http';
import { check, sleep } from 'k6';
import { client, login } from './lib/auth.js';
import { BASE_URL, buildOptions } from './lib/config.js';

// Each iteration creates one uniquely named job type and deactivates it; load/spike/soak runs leave deactivated rows behind.
export const options = buildOptions();

// The intentional 404/409 checks below are expected responses, not failures.
http.setResponseCallback(http.expectedStatuses({ min: 200, max: 299 }, 404, 409));

const API = `${BASE_URL}/api/v1/job-types`;
const json = { headers: { 'Content-Type': 'application/json' } };

export function setup() {
  return login();
}

export default function (token) {
  const api = client(token);
  const suffix = `${__VU}x${__ITER}x${Date.now().toString(36)}`;
  const name = `K6 ${suffix}`.slice(0, 100);

  const list = api.get(API);
  check(list, {
    'list: 200': (r) => r.status === 200,
    'list: has seeded job types': (r) => r.json().length >= 4,
  });

  const created = api.post(API, JSON.stringify({ name, laborCost: 1, profitPercent: 10 }), json);
  if (!check(created, { 'create: 201': (r) => r.status === 201 })) {
    return;
  }
  const id = created.json('id');

  check(api.get(`${API}/${id}`), { 'get: 200': (r) => r.status === 200 });
  check(api.get(`${API}/1/catalogs`), { 'catalogs: 200': (r) => r.status === 200 });
  check(api.put(`${API}/${id}`, JSON.stringify({ name, laborCost: 2 }), json), { 'update: 200': (r) => r.status === 200 });
  check(api.post(API, JSON.stringify({ name }), json), { 'duplicate name: 409': (r) => r.status === 409 });
  check(api.post(`${API}/${id}/deactivate`, null, json), { 'deactivate: 204': (r) => r.status === 204 });
  check(api.post(`${API}/${id}/activate`, null, json), { 'activate: 204': (r) => r.status === 204 });
  check(api.post(`${API}/${id}/deactivate`, null, json), { 'deactivate again: 204': (r) => r.status === 204 });
  check(api.get(`${API}/999999`), { 'unknown id: 404': (r) => r.status === 404 });

  sleep(1);
}
