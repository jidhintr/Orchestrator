import http from 'k6/http';
import { check, sleep } from 'k6';
import { BASE_URL, buildOptions } from './lib/config.js';

// Each iteration creates one catalog (left behind in load/spike/soak runs) and a chain for it that it deletes again.
export const options = buildOptions();

// The intentional 404/409 checks below are expected responses, not failures.
http.setResponseCallback(http.expectedStatuses({ min: 200, max: 299 }, 404, 409));

const API = `${BASE_URL}/api/v1/approval-chains`;
const json = { headers: { 'Content-Type': 'application/json', 'X-Acting-User-Id': '1' } };

export default function () {
  const suffix = `${__VU}X${__ITER}X${Date.now().toString(36).toUpperCase()}`;

  check(http.get(API), {
    'list: 200': (r) => r.status === 200,
    'list: has default': (r) => r.json().some((c) => c.isDefault),
  });
  check(http.get(`${API}/1`), { 'default: 200': (r) => r.status === 200 });

  const catalog = http.post(`${BASE_URL}/api/v1/catalogs`, JSON.stringify({ sku: `K6AC${suffix}`, name: `K6 chain ${suffix}`, jobTypeIds: [1] }), json);
  if (!check(catalog, { 'catalog: 201': (r) => r.status === 201 })) {
    return;
  }
  const catalogId = catalog.json('id');

  check(http.get(`${API}/for-catalog/${catalogId}`), { 'for catalog: default': (r) => r.status === 200 && r.json('isDefault') === true });

  const created = http.post(API, JSON.stringify({ name: `K6 ${suffix}`.slice(0, 80), catalogId, departmentIds: [3, 4] }), json);
  if (!check(created, { 'create: 201': (r) => r.status === 201 })) {
    return;
  }
  const id = created.json('id');

  check(http.get(`${API}/for-catalog/${catalogId}`), { 'for catalog: own': (r) => r.json('isDefault') === false });
  check(http.put(`${API}/${id}`, JSON.stringify({ name: `K6 ${suffix}`.slice(0, 80), departmentIds: [4, 3] }), json), { 'update: 200': (r) => r.status === 200 });
  check(http.post(API, JSON.stringify({ name: `K6 dup ${suffix}`.slice(0, 80), catalogId, departmentIds: [3] }), json), { 'second chain: 409': (r) => r.status === 409 });
  check(http.del(`${API}/1`, null, json), { 'default delete: 409': (r) => r.status === 409 });
  check(http.del(`${API}/${id}`, null, json), { 'delete: 204': (r) => r.status === 204 });
  check(http.get(`${API}/999999`), { 'unknown id: 404': (r) => r.status === 404 });

  sleep(1);
}
