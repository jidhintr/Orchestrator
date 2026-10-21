import http from 'k6/http';
import { check, sleep } from 'k6';
import { BASE_URL, buildOptions } from './lib/config.js';

// Each iteration creates one uniquely named department, so load/spike/soak runs leave rows behind in the target database.
export const options = buildOptions();

// The intentional 404 check below is an expected response, not a failure.
http.setResponseCallback(http.expectedStatuses({ min: 200, max: 299 }, 404));

const API = `${BASE_URL}/api/v1/departments`;
const json = { headers: { 'Content-Type': 'application/json', 'X-Acting-User-Id': '1' } };

export default function () {
  const suffix = `${__VU}X${__ITER}X${Date.now().toString(36).toUpperCase()}`;

  const list = http.get(API);
  check(list, {
    'list: 200': (r) => r.status === 200,
    'list: has seeded departments': (r) => r.json().length >= 6,
  });

  const created = http.post(API, JSON.stringify({ name: `K6 ${suffix}`, code: `K6${suffix}` }), json);
  if (!check(created, { 'create: 201': (r) => r.status === 201 })) {
    return;
  }
  const id = created.json('id');

  check(http.get(`${API}/${id}`), { 'get: 200': (r) => r.status === 200 });
  check(http.put(`${API}/${id}`, JSON.stringify({ name: `K6 renamed ${suffix}`, description: 'k6' }), json), {
    'update: 200': (r) => r.status === 200,
  });
  check(http.get(`${API}/2/roles`), { 'department roles: 200': (r) => r.status === 200 });
  check(http.post(`${API}/${id}/deactivate`, null, json), { 'deactivate: 204': (r) => r.status === 204 });
  check(http.get(`${API}/999999`), { 'unknown id: 404': (r) => r.status === 404 });

  sleep(1);
}
