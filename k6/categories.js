import http from 'k6/http';
import { check, sleep } from 'k6';
import { client, login } from './lib/auth.js';
import { BASE_URL, buildOptions } from './lib/config.js';

// Each iteration creates one uniquely named category and deactivates it; load/spike/soak runs leave deactivated rows behind.
export const options = buildOptions();

// The intentional 404/409 checks below are expected responses, not failures.
http.setResponseCallback(http.expectedStatuses({ min: 200, max: 299 }, 404, 409));

const API = `${BASE_URL}/api/v1/categories`;
const json = { headers: { 'Content-Type': 'application/json' } };

export function setup() {
  return login();
}

export default function (token) {
  const api = client(token);
  const name = `K6 ${__VU}x${__ITER}x${Date.now().toString(36)}`.slice(0, 100);

  check(api.get(API), { 'list: 200': (r) => r.status === 200 });

  const created = api.post(API, JSON.stringify({ name, description: 'k6' }), json);
  if (!check(created, { 'create: 201': (r) => r.status === 201 })) {
    return;
  }
  const id = created.json('id');

  check(api.get(`${API}/${id}`), { 'get: 200': (r) => r.status === 200 });
  check(api.put(`${API}/${id}`, JSON.stringify({ name }), json), { 'update: 200': (r) => r.status === 200 });
  check(api.post(API, JSON.stringify({ name }), json), { 'duplicate name: 409': (r) => r.status === 409 });
  check(api.post(`${API}/${id}/deactivate`), { 'deactivate: 204': (r) => r.status === 204 });
  check(api.post(`${API}/${id}/activate`), { 'activate: 204': (r) => r.status === 204 });
  check(api.post(`${API}/${id}/deactivate`), { 'deactivate again: 204': (r) => r.status === 204 });
  check(api.get(`${API}/999999`), { 'unknown id: 404': (r) => r.status === 404 });

  sleep(1);
}
