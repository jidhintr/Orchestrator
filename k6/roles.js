import http from 'k6/http';
import { check, sleep } from 'k6';
import { BASE_URL, buildOptions } from './lib/config.js';

// Each iteration creates and deletes one uniquely named role; the delete keeps load/spike/soak runs from leaving rows behind.
export const options = buildOptions();

// The intentional 400/404/409 checks below are expected responses, not failures.
http.setResponseCallback(http.expectedStatuses({ min: 200, max: 299 }, 400, 404, 409));

const API = `${BASE_URL}/api/v1/roles`;
const json = { headers: { 'Content-Type': 'application/json' } };

export default function () {
  const suffix = `${__VU}x${__ITER}x${Date.now().toString(36)}`;
  const name = `K6 ${suffix}`.slice(0, 50);

  const list = http.get(API);
  check(list, {
    'list: 200': (r) => r.status === 200,
    'list: has seeded roles': (r) => r.json().length >= 5,
  });

  const created = http.post(API, JSON.stringify({ name, description: 'k6' }), json);
  if (!check(created, { 'create: 201': (r) => r.status === 201 })) {
    return;
  }
  const id = created.json('id');

  check(http.get(`${API}/${id}`), { 'get: 200': (r) => r.status === 200 });
  check(http.get(`${API}/${id}/users`), { 'users: 200': (r) => r.status === 200 });
  check(http.put(`${API}/${id}`, JSON.stringify({ name, description: 'k6 updated' }), json), { 'update: 200': (r) => r.status === 200 });
  check(http.post(`${API}/${id}/disable`), { 'disable: 204': (r) => r.status === 204 });
  check(http.post(`${API}/${id}/enable`), { 'enable: 204': (r) => r.status === 204 });
  check(http.post(API, JSON.stringify({ name, description: 'k6' }), json), { 'duplicate name: 409': (r) => r.status === 409 });
  check(http.del(`${API}/2`), { 'system role delete: 409': (r) => r.status === 409 });
  check(http.del(`${API}/${id}`), { 'delete: 204': (r) => r.status === 204 });
  check(http.get(`${API}/999999`), { 'unknown id: 404': (r) => r.status === 404 });

  sleep(1);
}
