import http from 'k6/http';
import { check, sleep } from 'k6';
import { client, login } from './lib/auth.js';
import { BASE_URL, buildOptions } from './lib/config.js';

// Each iteration creates one uniquely addressed user, so load/spike/soak runs leave rows behind in the target database.
export const options = buildOptions();

// The intentional 400/401/403/404/409 checks below are expected responses, not failures.
http.setResponseCallback(http.expectedStatuses({ min: 200, max: 299 }, 400, 401, 403, 404, 409));

const API = `${BASE_URL}/api/v1/users`;
const json = { headers: { 'Content-Type': 'application/json' } };
const anonymous = { headers: { 'Content-Type': 'application/json' } };

export function setup() {
  return login();
}

export default function (token) {
  const api = client(token);
  const suffix = `${__VU}x${__ITER}x${Date.now().toString(36)}`;
  const body = { firstName: 'K6', lastName: `U${suffix}`.slice(0, 50), email: `k6.${suffix}@example.com`, mobile: `9${Date.now()}${__VU}${__ITER}`.slice(0, 20) };

  const list = api.get(API);
  check(list, {
    'list: 200': (r) => r.status === 200,
    'list: has seeded users': (r) => r.json().length >= 2,
  });

  const created = api.post(API, JSON.stringify(body), json);
  if (!check(created, { 'create: 201': (r) => r.status === 201 })) {
    return;
  }
  const id = created.json('id');

  check(api.get(`${API}/${id}`), { 'get: 200': (r) => r.status === 200 });
  check(api.put(`${API}/${id}/departments`, JSON.stringify({ departmentIds: [4, 2] }), json), { 'departments: 200': (r) => r.status === 200 });
  check(api.put(`${API}/${id}/roles`, JSON.stringify({ roles: ['Manager'] }), json), { 'roles: 200': (r) => r.status === 200 });
  check(api.put(`${API}/${id}`, JSON.stringify({ ...body, isActive: false }), json), { 'update: 200': (r) => r.status === 200 });
  check(api.post(API, JSON.stringify(body), json), { 'duplicate email: 409': (r) => r.status === 409 });
  check(api.put(`${API}/${id}/roles`, JSON.stringify({ roles: ['Wizard'] }), json), { 'unknown role: 400': (r) => r.status === 400 });
  check(http.post(API, JSON.stringify({ ...body, email: `a.${body.email}` }), anonymous), { 'no token: 401': (r) => r.status === 401 });
  check(api.put(`${API}/${id}`, JSON.stringify({ ...body, isActive: true }), json), { 'update again: 200': (r) => r.status === 200 });
  check(api.get(`${API}/999999`), { 'unknown id: 404': (r) => r.status === 404 });

  sleep(1);
}
