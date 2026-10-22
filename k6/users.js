import http from 'k6/http';
import { check, sleep } from 'k6';
import { BASE_URL, buildOptions } from './lib/config.js';

// Each iteration creates one uniquely addressed user, so load/spike/soak runs leave rows behind in the target database.
export const options = buildOptions();

// The intentional 400/401/403/404/409 checks below are expected responses, not failures.
http.setResponseCallback(http.expectedStatuses({ min: 200, max: 299 }, 400, 401, 403, 404, 409));

const API = `${BASE_URL}/api/v1/users`;
const asAdmin = { headers: { 'Content-Type': 'application/json', 'X-Acting-User-Id': '2' } };
const asSuperAdmin = { headers: { 'Content-Type': 'application/json', 'X-Acting-User-Id': '1' } };
const anonymous = { headers: { 'Content-Type': 'application/json' } };

export default function () {
  const suffix = `${__VU}x${__ITER}x${Date.now().toString(36)}`;
  const body = { firstName: 'K6', lastName: `U${suffix}`.slice(0, 50), email: `k6.${suffix}@example.com`, mobile: '9876543210' };

  const list = http.get(API);
  check(list, {
    'list: 200': (r) => r.status === 200,
    'list: has seeded users': (r) => r.json().length >= 2,
  });

  const created = http.post(API, JSON.stringify(body), asAdmin);
  if (!check(created, { 'create: 201': (r) => r.status === 201 })) {
    return;
  }
  const id = created.json('id');

  check(http.get(`${API}/${id}`), { 'get: 200': (r) => r.status === 200 });
  check(http.put(`${API}/${id}/departments`, JSON.stringify({ departmentIds: [4, 5] }), asAdmin), { 'departments: 200': (r) => r.status === 200 });
  check(http.put(`${API}/${id}/roles`, JSON.stringify({ roles: ['Manager'] }), asAdmin), { 'roles: 200': (r) => r.status === 200 });
  check(http.put(`${API}/${id}`, JSON.stringify({ ...body, isActive: false }), asAdmin), { 'update: 200': (r) => r.status === 200 });
  check(http.post(API, JSON.stringify(body), asAdmin), { 'duplicate email: 409': (r) => r.status === 409 });
  check(http.put(`${API}/${id}/roles`, JSON.stringify({ roles: ['Wizard'] }), asAdmin), { 'unknown role: 400': (r) => r.status === 400 });
  check(http.put(`${API}/1`, JSON.stringify({ ...body, isActive: true }), asAdmin), { 'admin edits super admin: 403': (r) => r.status === 403 });
  check(http.post(API, JSON.stringify({ ...body, email: `a.${body.email}` }), anonymous), { 'no acting user: 401': (r) => r.status === 401 });
  check(http.put(`${API}/${id}`, JSON.stringify({ ...body, isActive: true }), asSuperAdmin), { 'super admin edits: 200': (r) => r.status === 200 });
  check(http.get(`${API}/999999`), { 'unknown id: 404': (r) => r.status === 404 });

  sleep(1);
}
