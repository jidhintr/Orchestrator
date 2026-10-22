import http from 'k6/http';
import { check, sleep } from 'k6';
import { client, login } from './lib/auth.js';
import { BASE_URL, buildOptions } from './lib/config.js';

// Each iteration creates and deletes one uniquely named role; the delete keeps load/spike/soak runs from leaving rows behind.
export const options = buildOptions();

// The intentional 400/404/409 checks below are expected responses, not failures.
http.setResponseCallback(http.expectedStatuses({ min: 200, max: 299 }, 400, 404, 409));

const API = `${BASE_URL}/api/v1/roles`;
const json = { headers: { 'Content-Type': 'application/json' } };

export function setup() {
  return login();
}

export default function (token) {
  const api = client(token);
  const suffix = `${__VU}x${__ITER}x${Date.now().toString(36)}`;
  const name = `K6 ${suffix}`.slice(0, 50);

  const list = api.get(API);
  check(list, {
    'list: 200': (r) => r.status === 200,
    'list: has seeded roles': (r) => r.json().length >= 5,
  });

  const created = api.post(API, JSON.stringify({ name, description: 'k6' }), json);
  if (!check(created, { 'create: 201': (r) => r.status === 201 })) {
    return;
  }
  const id = created.json('id');

  check(api.get(`${API}/${id}`), { 'get: 200': (r) => r.status === 200 });
  check(api.get(`${API}/${id}/users`), { 'users: 200': (r) => r.status === 200 });
  check(api.put(`${API}/${id}`, JSON.stringify({ name, description: 'k6 updated' }), json), { 'update: 200': (r) => r.status === 200 });
  check(api.post(`${API}/${id}/disable`, null, json), { 'disable: 204': (r) => r.status === 204 });
  check(api.post(`${API}/${id}/enable`, null, json), { 'enable: 204': (r) => r.status === 204 });
  check(api.post(API, JSON.stringify({ name, description: 'k6' }), json), { 'duplicate name: 409': (r) => r.status === 409 });
  check(api.del(`${API}/2`, null, json), { 'system role delete: 409': (r) => r.status === 409 });
  check(api.del(`${API}/${id}`, null, json), { 'delete: 204': (r) => r.status === 204 });
  check(api.get(`${API}/999999`), { 'unknown id: 404': (r) => r.status === 404 });

  sleep(1);
}
