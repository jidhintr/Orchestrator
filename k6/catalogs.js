import http from 'k6/http';
import { check, sleep } from 'k6';
import { BASE_URL, buildOptions } from './lib/config.js';

// Each iteration creates two uniquely named catalogs, so load/spike/soak runs leave rows behind in the target database.
export const options = buildOptions();

// The intentional 404 check below is an expected response, not a failure.
http.setResponseCallback(http.expectedStatuses({ min: 200, max: 299 }, 404));

const API = `${BASE_URL}/api/v1/catalogs`;
const json = { headers: { 'Content-Type': 'application/json' } };

export default function () {
  const suffix = `${__VU}X${__ITER}X${Date.now().toString(36).toUpperCase()}`;

  check(http.get(API), { 'list: 200': (r) => r.status === 200 });

  const created = http.post(API, JSON.stringify({ sku: `K6${suffix}`, name: `K6 ${suffix}`, jobTypeIds: [1, 2, 4] }), json);
  if (!check(created, { 'create: 201': (r) => r.status === 201 })) {
    return;
  }
  const id = created.json('id');

  check(http.get(`${API}/${id}`), { 'get: 200': (r) => r.status === 200 });

  const cloned = http.post(`${API}/${id}/clone`, JSON.stringify({ sku: `K6C${suffix}`, name: `K6 clone ${suffix}` }), json);
  if (!check(cloned, { 'clone: 201': (r) => r.status === 201 })) {
    return;
  }
  const cloneId = cloned.json('id');

  check(http.put(`${API}/${cloneId}`, JSON.stringify({ sku: `K6C${suffix}`, name: `K6 clone ${suffix}`, jobTypeIds: [4, 1] }), json), {
    'update: 200': (r) => r.status === 200,
  });
  check(http.post(`${API}/${cloneId}/deactivate`), { 'deactivate: 204': (r) => r.status === 204 });
  check(http.get(`${API}/999999`), { 'unknown id: 404': (r) => r.status === 404 });

  sleep(1);
}
