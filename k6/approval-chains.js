import http from 'k6/http';
import { check, sleep } from 'k6';
import { client, login } from './lib/auth.js';
import { BASE_URL, buildOptions } from './lib/config.js';

// Each iteration creates one catalog (left behind in load/spike/soak runs) and a chain for it that it deletes again.
export const options = buildOptions();

// The intentional 404/409 checks below are expected responses, not failures.
http.setResponseCallback(http.expectedStatuses({ min: 200, max: 299 }, 404, 409));

const API = `${BASE_URL}/api/v1/approval-chains`;
const json = { headers: { 'Content-Type': 'application/json' } };

export function setup() {
  return login();
}

export default function (token) {
  const api = client(token);
  const suffix = `${__VU}X${__ITER}X${Date.now().toString(36).toUpperCase()}`;

  check(api.get(API), {
    'list: 200': (r) => r.status === 200,
    'list: has default': (r) => r.json().some((c) => c.isDefault),
  });
  check(api.get(`${API}/1`), { 'default: 200': (r) => r.status === 200 });

  const catalog = api.post(`${BASE_URL}/api/v1/catalogs`, JSON.stringify({ sku: `K6AC${suffix}`, name: `K6 chain ${suffix}`, jobTypeIds: [1] }), json);
  if (!check(catalog, { 'catalog: 201': (r) => r.status === 201 })) {
    return;
  }
  const catalogId = catalog.json('id');

  check(api.get(`${API}/for-catalog/${catalogId}`), { 'for catalog: default': (r) => r.status === 200 && r.json('isDefault') === true });

  const created = api.post(API, JSON.stringify({ name: `K6 ${suffix}`.slice(0, 80), catalogId, departmentIds: [3, 4] }), json);
  if (!check(created, { 'create: 201': (r) => r.status === 201 })) {
    return;
  }
  const id = created.json('id');

  check(api.get(`${API}/for-catalog/${catalogId}`), { 'for catalog: own': (r) => r.json('isDefault') === false });
  check(api.put(`${API}/${id}`, JSON.stringify({ name: `K6 ${suffix}`.slice(0, 80), departmentIds: [4, 3] }), json), { 'update: 200': (r) => r.status === 200 });
  check(api.post(API, JSON.stringify({ name: `K6 dup ${suffix}`.slice(0, 80), catalogId, departmentIds: [3] }), json), { 'second chain: 409': (r) => r.status === 409 });
  check(api.del(`${API}/1`, null, json), { 'default delete: 409': (r) => r.status === 409 });
  check(api.del(`${API}/${id}`, null, json), { 'delete: 204': (r) => r.status === 204 });
  check(api.get(`${API}/999999`), { 'unknown id: 404': (r) => r.status === 404 });

  sleep(1);
}
